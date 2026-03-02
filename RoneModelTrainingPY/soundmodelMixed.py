import random
import tensorflow as tf
from tensorflow.keras import layers, models
from tensorflow.keras.models import load_model
import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
import os
from sklearn.preprocessing import LabelEncoder
from sklearn.model_selection import train_test_split
from sklearn.utils import shuffle
from sklearn.metrics import confusion_matrix, classification_report
from scipy.ndimage import zoom
import seaborn as sns
import joblib
import sounddevice as sd
import librosa
from randomAddNoise import addNoise


MFCC_DIR = "mfcc_124d_gpu"
LABEL_PATH = "labels_124d.csv"
SAMPLE_RATE = 48000
N_MFCC = 40
MAX_FRAMES = 100

# Training acceleration configuration
EPOCHS = 30
BATCH_SIZE = 64
USE_MIXED_PRECISION = True  # Enable mixed precision acceleration

FEATURE_DIMS = {0: 40, 1: 41, 2: 123, 3: 124}
CURRENT_FEATURE_DIM = FEATURE_DIMS[3]

N_FFT = 2048
HOP_LENGTH = 480

# Enable mixed precision (2-3x speedup on RTX GPUs)
if USE_MIXED_PRECISION:
    policy = tf.keras.mixed_precision.Policy('mixed_float16')
    tf.keras.mixed_precision.set_global_policy(policy)
    print("Mixed precision enabled")


def load_mfcc_data(mfcc_dir, label_path, max_frames, augment=False):
    """Load MFCC data with optional augmentation"""
    labels_df = pd.read_csv(label_path)

    if augment:
        return load_mfcc_data_augmented(mfcc_dir, labels_df, max_frames)

    mfcc_filenames = labels_df["mfcc_filename"].tolist()
    name_labels = labels_df["name_label"].tolist()

    le = LabelEncoder()
    numeric_labels = le.fit_transform(name_labels)
    num_classes = len(le.classes_)
    print(f"Classes: {num_classes}, List: {le.classes_}")

    mfcc_features = []
    for filename in mfcc_filenames:
        mfcc_path = os.path.join(mfcc_dir, filename)
        mfcc = np.load(mfcc_path)

        if mfcc.shape[0] < max_frames:
            pad_width = ((0, max_frames - mfcc.shape[0]), (0, 0))
            mfcc_padded = np.pad(mfcc, pad_width, mode="constant")
        else:
            mfcc_padded = mfcc[:max_frames, :]

        mfcc_features.append(mfcc_padded)

    mfcc_features = np.array(mfcc_features)
    mfcc_features = np.expand_dims(mfcc_features, axis=-1)
    mfcc_features, numeric_labels = shuffle(mfcc_features, numeric_labels, random_state=42)
    return mfcc_features, numeric_labels, num_classes, le


def load_mfcc_data_augmented(mfcc_dir, labels_df, max_frames):
    """Data augmentation for easily confused classes"""
    boost_classes = ['Dagaz', 'Ehwaz', 'Ingwaz', 'Othala', 'Mannaz']

    mfcc_features = []
    numeric_labels = []

    le = LabelEncoder()
    le.fit(labels_df['name_label'])

    for label in le.classes_:
        class_samples = labels_df[labels_df['name_label'] == label]

        for _, row in class_samples.iterrows():
            mfcc_path = os.path.join(mfcc_dir, row['mfcc_filename'])
            mfcc = np.load(mfcc_path)

            # Original sample
            mfcc_features.append(mfcc)
            numeric_labels.append(le.transform([label])[0])

            # Augment easily confused classes
            if label in boost_classes:
                # Time shifting
                for shift in [-5, -3, 3, 5]:
                    mfcc_shifted = np.roll(mfcc, shift, axis=0)
                    mfcc_features.append(mfcc_shifted)
                    numeric_labels.append(le.transform([label])[0])

                # Noise injection
                for noise_level in [0.005, 0.01]:
                    noise = np.random.randn(*mfcc.shape) * noise_level
                    mfcc_features.append(mfcc + noise)
                    numeric_labels.append(le.transform([label])[0])

                # Time stretching
                for zoom_factor in [0.9, 1.1]:
                    mfcc_stretched = zoom(mfcc, (zoom_factor, 1), order=1)
                    if mfcc_stretched.shape[0] > mfcc.shape[0]:
                        mfcc_stretched = mfcc_stretched[:mfcc.shape[0], :]
                    else:
                        pad = ((0, mfcc.shape[0] - mfcc_stretched.shape[0]), (0, 0))
                        mfcc_stretched = np.pad(mfcc_stretched, pad, mode='edge')
                    mfcc_features.append(mfcc_stretched)
                    numeric_labels.append(le.transform([label])[0])

    print(f"Augmented: {len(labels_df)} → {len(mfcc_features)} samples")

    # Standardize frame count
    mfcc_padded = []
    for mfcc in mfcc_features:
        if mfcc.shape[0] < max_frames:
            pad = ((0, max_frames - mfcc.shape[0]), (0, 0))
            mfcc_padded.append(np.pad(mfcc, pad, mode="constant"))
        else:
            mfcc_padded.append(mfcc[:max_frames, :])

    mfcc_padded = np.array(mfcc_padded)
    mfcc_padded = np.expand_dims(mfcc_padded, axis=-1)
    numeric_labels = np.array(numeric_labels)
    mfcc_padded, numeric_labels = shuffle(mfcc_padded, numeric_labels, random_state=42)

    return mfcc_padded, numeric_labels, len(le.classes_), le


def getTrainList(mfcc_features, numeric_labels, num_classes, le):
    """Split data into training and testing sets"""
    x_train, x_test, y_train, y_test = train_test_split(
        mfcc_features, numeric_labels, test_size=0.2, random_state=42, stratify=numeric_labels
    )
    return x_train, x_test, y_train, y_test, num_classes, le


# ==================== Model Definition ====================

def setModeResNet(num_classes):
    """Lightweight ResNet-style CNN for accelerated training"""

    def residual_block(x, filters, kernel_size=3, stride=1):
        shortcut = x

        x = layers.Conv2D(filters, kernel_size, strides=stride, padding='same', use_bias=False)(x)
        x = layers.BatchNormalization()(x)
        x = layers.ReLU()(x)

        x = layers.Conv2D(filters, kernel_size, padding='same', use_bias=False)(x)
        x = layers.BatchNormalization()(x)

        if shortcut.shape[-1] != filters or stride != 1:
            shortcut = layers.Conv2D(filters, 1, strides=stride, padding='same', use_bias=False)(shortcut)
            shortcut = layers.BatchNormalization()(shortcut)

        x = layers.Add()([x, shortcut])
        x = layers.ReLU()(x)
        return x

    inputs = layers.Input(shape=(MAX_FRAMES, CURRENT_FEATURE_DIM, 1))

    x = layers.Conv2D(32, 3, padding='same', use_bias=False)(inputs)
    x = layers.BatchNormalization()(x)
    x = layers.ReLU()(x)

    x = residual_block(x, 32)
    x = residual_block(x, 32)
    x = layers.MaxPooling2D(2)(x)
    x = layers.Dropout(0.3)(x)

    x = residual_block(x, 64, stride=2)
    x = residual_block(x, 64)
    x = layers.MaxPooling2D(2)(x)
    x = layers.Dropout(0.3)(x)

    x = residual_block(x, 128, stride=2)
    x = residual_block(x, 128)
    x = layers.Dropout(0.4)(x)

    x = layers.GlobalAveragePooling2D()(x)
    x = layers.Dense(256, activation='relu')(x)
    x = layers.Dropout(0.5)(x)
    # Mixed precision requires float32 output layer
    outputs = layers.Dense(num_classes, dtype='float32')(x)

    model = models.Model(inputs, outputs)
    return model


# ==================== Weights and Training ====================

def get_aggressive_weights(label_encoder, y_train):
    """Aggressive class weight adjustment"""
    from sklearn.utils.class_weight import compute_class_weight

    class_weights = compute_class_weight('balanced', classes=np.unique(y_train), y=y_train)
    class_weight_dict = dict(enumerate(class_weights))

    # Significantly reduce weight for Laguz
    laguz_idx = np.where(label_encoder.classes_ == 'Laguz')[0][0]
    class_weight_dict[laguz_idx] *= 0.6
    print(f"  Laguz: {class_weight_dict[laguz_idx]:.3f}")

    # Increase weights for easily confused classes
    vulnerable = {'Dagaz': 1.2, 'Ehwaz': 1.2, 'Ingwaz': 1.2, 'Othala': 1.1, 'Mannaz': 1.1}
    for cls_name, mult in vulnerable.items():
        if cls_name in label_encoder.classes_:
            idx = np.where(label_encoder.classes_ == cls_name)[0][0]
            class_weight_dict[idx] *= mult
            print(f"  {cls_name}: {class_weight_dict[idx]:.3f}")

    return class_weight_dict


def trainAndSaved(mfcc_dir="mfcc_124d_gpu",
                  label_path="mfcc_124d_gpu/labels_124d.csv",
                  model_save_path="rune_resnet_best.keras",
                  use_augment=False,
                  use_aggressive_weights=True,
                  epochs=EPOCHS,
                  batch_size=BATCH_SIZE,
                  seed=42):
    """Training function - fixed version"""

    np.random.seed(seed)
    tf.random.set_seed(seed)
    random.seed(seed)

    # Load data
    mfcc_features, numeric_labels, num_classes, le = load_mfcc_data(
        mfcc_dir, label_path, MAX_FRAMES, augment=use_augment
    )
    x_train, x_test, y_train, y_test, num_classes, label_encoder = getTrainList(
        mfcc_features, numeric_labels, num_classes, le
    )

    print(f"\nTrain: {x_train.shape}, Test: {x_test.shape}")

    model = setModeResNet(num_classes)
    model.summary()

    # Optimizer - only use CosineDecayRestarts, remove ReduceLROnPlateau
    lr_schedule = tf.keras.optimizers.schedules.CosineDecayRestarts(
        initial_learning_rate=0.001, first_decay_steps=10, t_mul=2.0, m_mul=0.9, alpha=0.0001
    )
    optimizer = tf.keras.optimizers.AdamW(learning_rate=lr_schedule, weight_decay=0.001)

    # Mixed precision requires jit_compile
    model.compile(
        optimizer=optimizer,
        loss=tf.keras.losses.SparseCategoricalCrossentropy(from_logits=True),
        metrics=['accuracy'],
        jit_compile=True  # XLA acceleration
    )

    # Removed ReduceLROnPlateau to avoid conflict with learning rate scheduler
    callbacks = [
        tf.keras.callbacks.EarlyStopping(monitor='val_accuracy', patience=10, restore_best_weights=True, verbose=1),
        tf.keras.callbacks.ModelCheckpoint(model_save_path, monitor='val_accuracy', save_best_only=True, verbose=1)
    ]

    # Class weights
    class_weight = None
    if use_aggressive_weights:
        print("\nClass weights:")
        class_weight = get_aggressive_weights(label_encoder, y_train)

    # Training
    history = model.fit(
        x_train, y_train,
        epochs=epochs,
        batch_size=batch_size,
        validation_data=(x_test, y_test),
        callbacks=callbacks,
        class_weight=class_weight,
        verbose=1
    )

    # Plot training curves
    plt.figure(figsize=(15, 4))
    plt.subplot(1, 3, 1)
    plt.plot(history.history['accuracy'], label='train')
    plt.plot(history.history['val_accuracy'], label='val')
    plt.legend()
    plt.title('Accuracy')
    plt.subplot(1, 3, 2)
    plt.plot(history.history['loss'], label='train')
    plt.plot(history.history['val_loss'], label='val')
    plt.legend()
    plt.title('Loss')
    plt.subplot(1, 3, 3)
    plt.text(0.5, 0.5, 'CosineDecayRestarts\n(learning rate scheduled)',
             ha='center', va='center', transform=plt.gca().transAxes)
    plt.title('Learning Rate')
    plt.tight_layout()
    plt.savefig('training_curves.png', dpi=150)
    plt.show()

    # Evaluation
    test_loss, test_acc = model.evaluate(x_test, y_test, verbose=2)
    print(f'\nTest accuracy: {test_acc:.4f}')

    model.save(model_save_path)
    joblib.dump(label_encoder, "label_encoder.pkl")
    print(f"\nSaved: {model_save_path}")

    return model, label_encoder


# ==================== Ensemble Training and Prediction ====================

def train_ensemble(mfcc_dir, label_path, n_models=3, seeds=[42, 123, 456]):
    """Train ensemble models"""
    model_paths = []
    for i, seed in enumerate(seeds[:n_models]):
        path = f"rune_ensemble_{i}.keras"
        print(f"\n{'=' * 40}\nTraining model {i + 1}/{n_models}, seed={seed}")
        trainAndSaved(
            mfcc_dir=mfcc_dir,
            label_path=label_path,
            model_save_path=path,
            use_augment=(i == 0),  # First model uses augmented data
            use_aggressive_weights=True,
            epochs=EPOCHS,
            batch_size=BATCH_SIZE,
            seed=seed
        )
        model_paths.append(path)
    return model_paths


def final_ensemble_predict(model_paths, encoder_path, x,
                           laguz_votes=2, ingwaz_votes=2,
                           laguz_threshold=0.6):
    """
    Final ensemble prediction
    - Laguz: requires laguz_votes votes and probability > laguz_threshold
    - Ingwaz: requires ingwaz_votes votes
    """
    models = [load_model(p) for p in model_paths]
    encoder = joblib.load(encoder_path)

    all_probs = []
    all_preds = []

    for model in models:
        preds = model.predict(x, verbose=0)
        probs = tf.nn.softmax(preds).numpy()
        all_probs.append(probs)
        all_preds.append(np.argmax(probs, axis=1))

    # Average probabilities
    avg_probs = np.mean(all_probs, axis=0)

    # Get class indices
    laguz_idx = np.where(encoder.classes_ == 'Laguz')[0][0]
    ingwaz_idx = np.where(encoder.classes_ == 'Ingwaz')[0][0]

    # Apply suppression strategy
    for i in range(len(avg_probs)):
        # Laguz suppression
        laguz_v = sum(1 for p in all_preds if p[i] == laguz_idx)
        if laguz_v < laguz_votes or avg_probs[i][laguz_idx] < laguz_threshold:
            avg_probs[i][laguz_idx] *= 0.3

        # Ingwaz suppression
        ingwaz_v = sum(1 for p in all_preds if p[i] == ingwaz_idx)
        if ingwaz_v < ingwaz_votes:
            avg_probs[i][ingwaz_idx] *= 0.7

    # Re-normalize
    avg_probs = avg_probs / avg_probs.sum(axis=1, keepdims=True)
    predicted_labels = np.argmax(avg_probs, axis=1)

    return encoder.inverse_transform(predicted_labels), avg_probs, encoder


# ==================== Evaluation ====================

def evaluate_prediction(y_true, y_pred, class_names, save_prefix="result"):
    """Comprehensive evaluation"""
    cm = confusion_matrix(y_true, y_pred, labels=class_names)
    cm_percent = cm.astype('float') / (cm.sum(axis=1)[:, np.newaxis] + 1e-8) * 100

    # Calculate metrics
    diag = np.diag(cm)
    precisions = diag / (cm.sum(axis=0) + 1e-8)
    recalls = diag / (cm.sum(axis=1) + 1e-8)
    f1s = 2 * (precisions * recalls) / (precisions + recalls + 1e-8)

    # Display sorted metrics
    print("\n" + "=" * 70)
    print("Per-Class Metrics (by F1):")
    print("=" * 70)
    for idx in np.argsort(f1s):
        print(f"{class_names[idx]:12s} | P: {precisions[idx]:.3f} | R: {recalls[idx]:.3f} | F1: {f1s[idx]:.3f}")

    # Plot confusion matrix
    plt.figure(figsize=(14, 12))
    sns.heatmap(cm_percent, annot=True, fmt='.1f', cmap='YlOrRd',
                xticklabels=class_names, yticklabels=class_names,
                square=True, vmin=0, vmax=100)
    plt.title('Confusion Matrix (%)', fontsize=14, fontweight='bold')
    plt.xlabel('Predicted')
    plt.ylabel('True')
    plt.xticks(rotation=45, ha='right')
    plt.yticks(rotation=0)
    plt.tight_layout()
    plt.savefig(f'{save_prefix}_cm.png', dpi=300, bbox_inches='tight')
    plt.show()

    # Classification report
    print("\n" + "=" * 70)
    print(classification_report(y_true, y_pred, target_names=class_names))

    # Confused pairs
    print("\n" + "=" * 70)
    print("Top 10 Confused Pairs:")
    confused = []
    for i in range(len(class_names)):
        for j in range(len(class_names)):
            if i != j and cm[i, j] > 0:
                confused.append((class_names[i], class_names[j], cm[i, j], cm_percent[i, j]))
    confused.sort(key=lambda x: x[2], reverse=True)
    for t, p, c, pct in confused[:10]:
        print(f"  {t} → {p}: {c} times ({pct:.1f}%)")

    return 100.0 * np.sum(diag) / np.sum(cm)


# ==================== Recording and Real-time Prediction ====================

def recordAndGetMfcc(duration=1.5,
                     sample_rate=SAMPLE_RATE,
                     n_mfcc=N_MFCC,
                     max_frames=MAX_FRAMES,
                     is_add_noise=False,
                     noise_limit=(5, 10),
                     energy_type=3,
                     need_spectral_subtraction=False):
    """Record audio and extract MFCC features"""

    if need_spectral_subtraction:
        print("Recording noise sample...")
        noise_recording = sd.rec(int(duration * sample_rate), samplerate=sample_rate, channels=1, dtype='float32')
        sd.wait()
        noise_data = noise_recording.squeeze()

    print(f"Recording {duration}s...")
    recording = sd.rec(int(duration * sample_rate), samplerate=sample_rate, channels=1, dtype='float32')
    sd.wait()
    audio_data = recording.squeeze()

    if need_spectral_subtraction and 'noise_data' in locals():
        stft_audio = librosa.stft(audio_data, n_fft=N_FFT, hop_length=HOP_LENGTH)
        stft_noise = librosa.stft(noise_data, n_fft=N_FFT, hop_length=HOP_LENGTH)
        mag_audio = np.abs(stft_audio)
        phase_audio = np.angle(stft_audio)
        mag_noise_avg = np.mean(np.abs(stft_noise), axis=1, keepdims=True)
        mag_denoised = np.maximum(mag_audio - mag_noise_avg, 0.1 * mag_noise_avg)
        stft_denoised = mag_denoised * np.exp(1j * phase_audio)
        audio_data = librosa.istft(stft_denoised, hop_length=HOP_LENGTH)

    sd.play(audio_data, sample_rate)
    sd.wait()

    if is_add_noise:
        noise_adder = addNoise()
        audio_data = noise_adder.addNoise(audio_data, random.choice(["add noise", "echo"]), noise_limit)

    audio_trimmed, _ = librosa.effects.trim(audio_data, top_db=30)
    if len(audio_trimmed) == 0:
        audio_trimmed = audio_data

    # Extract features (energy_type=3)
    mfcc = librosa.feature.mfcc(y=audio_trimmed, sr=sample_rate, n_mfcc=n_mfcc, n_fft=N_FFT, hop_length=HOP_LENGTH)
    energy = librosa.feature.rms(y=audio_trimmed, frame_length=N_FFT, hop_length=HOP_LENGTH)
    base = np.concatenate([mfcc, energy], axis=0)
    delta1 = librosa.feature.delta(base, order=1, mode='nearest')
    delta2 = librosa.feature.delta(base, order=2, mode='nearest')
    feat_123 = np.concatenate([base, delta1, delta2], axis=0).T
    onset_env = librosa.onset.onset_strength(y=audio_trimmed, sr=sample_rate, hop_length=HOP_LENGTH)
    cp_count = len(librosa.onset.onset_detect(onset_envelope=onset_env, sr=sample_rate, hop_length=HOP_LENGTH))
    feat = np.concatenate([feat_123, np.full((feat_123.shape[0], 1), cp_count)], axis=1)

    # Normalization and padding
    feat = (feat - np.mean(feat)) / (np.std(feat) + 1e-8)
    if feat.shape[0] < max_frames:
        feat = np.pad(feat, ((0, max_frames - feat.shape[0]), (0, 0)), mode='constant')
    else:
        feat = feat[:max_frames, :]

    return np.expand_dims(np.expand_dims(feat, axis=0), axis=-1)


def predict_from_mic_ensemble(model_paths, encoder_path, duration=1.5, **kwargs):
    """Ensemble model prediction from microphone"""
    mfcc_input = recordAndGetMfcc(duration=duration)

    pred_names, probs, encoder = final_ensemble_predict(
        model_paths, encoder_path, mfcc_input,** kwargs
    )

    predicted_name = pred_names[0]
    confidence = np.max(probs[0])

    print(f"\nPredicted: {predicted_name} (confidence: {confidence:.3f})")

    # Show alternatives for low confidence
    if confidence < 0.7:
        top3 = np.argsort(probs[0])[-3:][::-1]
        print("Alternatives:")
        for idx in top3:
            print(f"  {encoder.classes_[idx]}: {probs[0][idx]:.3f}")

    return predicted_name, confidence


# ==================== Main Program ====================

if __name__ == "__main__":
    # Configuration
    MODEL_PATHS = ["rune_ensemble_0.keras", "rune_ensemble_1.keras", "rune_ensemble_2.keras"]

    # 2. Ensemble model evaluation (recommended)
    print("Loading test data...")
    test_features, test_labels, _, _ = load_mfcc_data(
        "mfcc_124d_gpu", "mfcc_124d_gpu/labels_124d.csv", MAX_FRAMES
    )

    print("\nRunning ensemble prediction...")
    pred_names, probs, encoder = final_ensemble_predict(
        MODEL_PATHS, "label_encoder.pkl", test_features,
        laguz_votes=2,  # Laguz requires 2 votes
        ingwaz_votes=2,  # Ingwaz requires 2 votes
        laguz_threshold=0.6  # Laguz probability threshold
    )

    true_names = encoder.inverse_transform(test_labels)
    acc = evaluate_prediction(true_names, pred_names, encoder.classes_, "final_ensemble")
    print(f"\n{'=' * 70}")
    print(f"FINAL ENSEMBLE ACCURACY: {acc:.2f}%")
    print(f"{'=' * 70}")

    # 3. Real-time microphone prediction (ensemble version)
    """
    print("\nStarting real-time prediction...")
    while True:
        input("\nPress Enter to record (Ctrl+C to exit)...")
        name, conf = predict_from_mic_ensemble(
            MODEL_PATHS, "label_encoder.pkl",
            duration=1.5,
            laguz_votes=2,
            ingwaz_votes=2
        )
    """