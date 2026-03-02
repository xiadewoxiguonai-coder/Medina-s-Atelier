import numpy as np
import os
import csv
from tqdm import tqdm
import wave
import struct
import torch
import torch.nn.functional as F

# ==================== Configuration ====================
SAMPLE_RATE = 48000
N_MFCC = 40
N_FFT = 2048
HOP_LENGTH = 480
N_MELS = 128
MAX_FRAMES = 100
TOP_DB = 25
BATCH_SIZE = 32

# Device configuration
DEVICE = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
print(f"Using device: {DEVICE}")
if DEVICE.type == 'cuda':
    print(f"GPU: {torch.cuda.get_device_name(0)}")
    print(f"GPU Memory: {torch.cuda.get_device_properties(0).total_memory / 1e9:.1f} GB")


class MFCCExtractor124D_GPU_Batch:
    """GPU Batch MFCC Extractor"""

    def __init__(self):
        self.sample_rate = SAMPLE_RATE
        self.n_mfcc = N_MFCC
        self.n_fft = N_FFT
        self.hop_length = HOP_LENGTH
        self.n_mels = N_MELS
        self.max_frames = MAX_FRAMES

        # Precompute GPU tensors
        self.hann_window = torch.hann_window(N_FFT, dtype=torch.float32, device=DEVICE)
        self.mel_filter_bank = self._create_mel_filter_bank().to(DEVICE)
        self.dct_matrix = self._create_dct_matrix().to(DEVICE)

    def _hz_to_mel(self, hz):
        f_min, f_sp = 0.0, 200.0 / 3.0
        min_log_hz, min_log_mel = 1000.0, (1000.0 - f_min) / f_sp
        logstep = np.log(6.4) / 27.0
        return np.where(hz >= min_log_hz,
                        min_log_mel + np.log(hz / min_log_hz) / logstep,
                        (hz - f_min) / f_sp)

    def _mel_to_hz(self, mel):
        f_min, f_sp = 0.0, 200.0 / 3.0
        min_log_hz, min_log_mel = 1000.0, (1000.0 - f_min) / f_sp
        logstep = np.log(6.4) / 27.0
        return np.where(mel >= min_log_mel,
                        min_log_hz * np.exp(logstep * (mel - min_log_mel)),
                        f_min + f_sp * mel)

    def _create_mel_filter_bank(self):
        n_freqs = N_FFT // 2 + 1
        mel_filter_bank = np.zeros((N_MELS, n_freqs), dtype=np.float32)

        f_min, f_max = 0.0, SAMPLE_RATE / 2.0
        mel_min, mel_max = self._hz_to_mel(f_min), self._hz_to_mel(f_max)
        mel_points = np.linspace(mel_min, mel_max, N_MELS + 2)
        freq_points = self._mel_to_hz(mel_points)

        fft_freqs = np.linspace(0, SAMPLE_RATE / 2, n_freqs)

        for i in range(N_MELS):
            left, center, right = freq_points[i], freq_points[i + 1], freq_points[i + 2]
            mask = (fft_freqs > left) & (fft_freqs <= center)
            mel_filter_bank[i, mask] = (fft_freqs[mask] - left) / (center - left)
            mask = (fft_freqs > center) & (fft_freqs < right)
            mel_filter_bank[i, mask] = (right - fft_freqs[mask]) / (right - center)
            area = (right - left) / 2.0
            if area > 0:
                mel_filter_bank[i] /= area

        return torch.tensor(mel_filter_bank, dtype=torch.float32, device=DEVICE)

    def _create_dct_matrix(self):
        n = np.arange(N_MELS)
        k = np.arange(N_MFCC).reshape(-1, 1)
        dct_matrix = np.cos(np.pi * (n + 0.5) * k / N_MELS)
        dct_matrix[0] *= 1.0 / np.sqrt(N_MELS)
        dct_matrix[1:] *= np.sqrt(2.0 / N_MELS)
        return torch.tensor(dct_matrix, dtype=torch.float32, device=DEVICE)

    def _trim_silence(self, audio, top_db=25):
        """Remove silence from single audio file"""
        frame_length, hop_len = N_FFT, 512
        n_frames = 1 + (len(audio) - frame_length) // hop_len
        if n_frames <= 0:
            return audio

        frames = np.lib.stride_tricks.sliding_window_view(audio, frame_length)[::hop_len][:n_frames]
        rms = np.sqrt(np.mean(frames ** 2, axis=1))

        db = 20.0 * np.log10(rms + 1e-10)
        max_db = np.max(db)
        threshold = max_db - top_db

        valid_frames = np.where(db > threshold)[0]
        if len(valid_frames) == 0:
            return audio

        start_frame, end_frame = valid_frames[0], valid_frames[-1]
        start_sample = start_frame * hop_len
        end_sample = min((end_frame + 1) * hop_len + frame_length, len(audio))

        return audio[start_sample:end_sample]

    def _extract_single_gpu(self, audio):
        """Extract MFCC for single audio on GPU (avoid batch length mismatch)"""
        # Padding
        pad_length = N_FFT // 2
        padded = np.pad(audio, (pad_length, pad_length), mode='constant')

        # Transfer to GPU
        audio_tensor = torch.from_numpy(padded).float().to(DEVICE)

        # Framing
        n_frames = (len(padded) - N_FFT) // HOP_LENGTH + 1
        frames = audio_tensor.unfold(0, N_FFT, HOP_LENGTH)[:n_frames]

        # Apply window
        windowed = frames * self.hann_window

        # FFT
        fft_result = torch.fft.rfft(windowed, dim=-1)
        power_spec = torch.abs(fft_result) ** 2

        # Mel filtering
        mel_spec = torch.matmul(power_spec, self.mel_filter_bank.T)

        # Convert to dB and clamp
        mel_spec_db = 10.0 * torch.log10(mel_spec + 1e-10)
        mel_spec_db = torch.clamp(mel_spec_db, min=-55.75132)

        # DCT transform
        mfcc = torch.matmul(mel_spec_db, self.dct_matrix.T)

        return mfcc.cpu().numpy()

    def _extract_rms_gpu(self, audio):
        """Extract RMS for single audio on GPU"""
        pad_length = N_FFT // 2
        padded = np.pad(audio, (pad_length, pad_length), mode='constant')

        audio_tensor = torch.from_numpy(padded).float().to(DEVICE)
        n_frames = (len(padded) - N_FFT) // HOP_LENGTH + 1
        frames = audio_tensor.unfold(0, N_FFT, HOP_LENGTH)[:n_frames]

        rms = torch.sqrt(torch.mean(frames ** 2, dim=-1))
        return rms.cpu().numpy()

    def _compute_delta(self, features, order=1):
        """Compute delta features"""
        n_frames, n_dims = features.shape
        delta = np.zeros_like(features)
        width, half = 9, 4

        for d in range(n_dims):
            for t in range(n_frames):
                s = 0.0
                norm = 0.0
                for i in range(-half, half + 1):
                    idx = max(0, min(t + i, n_frames - 1))
                    weight = i if order == 1 else i * i
                    s += weight * features[idx, d]
                    norm += weight * weight
                delta[t, d] = s / (norm + 1e-8)

        return delta

    def _detect_onset(self, audio):
        """Detect onset change points"""
        onset_hop_length = 512
        pad_length = N_FFT // 2
        padded = np.pad(audio, (pad_length, pad_length), mode='constant')
        n_frames = 1 + (len(padded) - N_FFT) // onset_hop_length

        if n_frames <= 0:
            return 0

        hann = self.hann_window.cpu().numpy()
        frames = np.lib.stride_tricks.sliding_window_view(padded, N_FFT)[::onset_hop_length][:n_frames]
        windowed = frames * hann

        fft_result = np.fft.rfft(windowed, axis=-1)
        mag = np.abs(fft_result)

        flux = np.sum(np.maximum(mag[1:] - mag[:-1], 0), axis=-1)
        onset_env = np.concatenate([[0], flux])

        mean, std = np.mean(onset_env), np.std(onset_env)
        threshold = mean + 0.07 * std

        wait = 3
        count = 0
        last_onset = -wait

        for i in range(3, len(onset_env) - 3):
            if i - last_onset < wait:
                continue
            is_max = np.all(onset_env[i] > onset_env[i - 3:i]) and np.all(onset_env[i] > onset_env[i + 1:i + 4])
            if is_max and onset_env[i] > threshold:
                count += 1
                last_onset = i

        return count

    def extract(self, audio):
        """Extract 124-dimensional MFCC features for single audio"""
        # Remove silence
        y = self._trim_silence(audio, TOP_DB)
        if len(y) < HOP_LENGTH * 3:
            return None

        # GPU extraction
        mfcc = self._extract_single_gpu(y)  # [n_frames, 40]
        rms = self._extract_rms_gpu(y)  # [n_frames]
        n_frames = mfcc.shape[0]

        # Base features [n_frames, 41]
        base_features = np.column_stack([mfcc, rms])

        # Compute delta features
        delta1 = self._compute_delta(base_features, 1)
        delta2 = self._compute_delta(base_features, 2)

        # Combine to 123 dimensions
        features_123 = np.zeros((n_frames, 123), dtype=np.float32)
        features_123[:, :41] = base_features
        features_123[:, 41:82] = delta1
        features_123[:, 82:123] = delta2

        # Count change points
        change_point_count = self._detect_onset(y)

        # Extend to 124 dimensions
        features_124 = np.zeros((n_frames, 124), dtype=np.float32)
        features_124[:, :123] = features_123
        features_124[:, 123] = change_point_count

        # Pad or truncate to max frames
        result = np.zeros((MAX_FRAMES, 124), dtype=np.float32)
        copy_frames = min(n_frames, MAX_FRAMES)
        result[:copy_frames] = features_124[:copy_frames]

        return result


def load_wav_fast(filepath):
    """Fast WAV file loading"""
    with wave.open(filepath, 'rb') as wf:
        n_channels = wf.getnchannels()
        sample_width = wf.getsampwidth()
        sample_rate = wf.getframerate()
        n_frames = wf.getnframes()

        raw_data = wf.readframes(n_frames)

        if sample_width == 2:
            audio = np.frombuffer(raw_data, dtype=np.int16).astype(np.float32) / 32768.0
        else:
            raise ValueError(f"Unsupported sample width: {sample_width}")

        if n_channels == 2:
            audio = audio.reshape(-1, 2).mean(axis=1)

        if sample_rate != SAMPLE_RATE:
            new_length = int(len(audio) * SAMPLE_RATE / sample_rate)
            audio = np.interp(np.linspace(0, len(audio) - 1, new_length),
                              np.arange(len(audio)), audio)

        return audio


def process_folder_gpu(audio_root, output_dir, label_file):
    """Process audio folder using GPU (process one by one to avoid length mismatch)"""
    os.makedirs(output_dir, exist_ok=True)

    # Collect all files
    voice_types = ['RuneAudioRecords_Male', 'RuneAudioRecords_Female', 'RuneAudioRecords_Elderly']
    all_files = []

    for voice_type in voice_types:
        voice_folder = os.path.join(audio_root, voice_type)
        if not os.path.exists(voice_folder):
            continue

        audio_files = [f for f in os.listdir(voice_folder) if f.lower().endswith('.wav')]
        for f in audio_files:
            all_files.append((os.path.join(voice_folder, f), voice_type))

    print(f"Total {len(all_files)} files, processing one by one with GPU")

    # Initialize GPU extractor
    extractor = MFCCExtractor124D_GPU_Batch()

    all_labels = []
    total_processed = 0

    # Process one by one (GPU is fast, no need for batching)
    for filepath, voice_type in tqdm(all_files, desc="GPU Processing"):
        try:
            # Load audio
            audio = load_wav_fast(filepath)

            # Extract features with GPU
            features = extractor.extract(audio)
            if features is None:
                continue

            # Save features
            base_name = os.path.splitext(os.path.basename(filepath))[0]
            mfcc_filename = f"{base_name}_124d.npy"
            mfcc_path = os.path.join(output_dir, mfcc_filename)

            np.save(mfcc_path, features)

            name_label = base_name.split('_')[0]
            all_labels.append({
                'mfcc_filename': mfcc_filename,
                'name_label': name_label,
                'voice_type': voice_type,
                'original_file': base_name,
                'frames': MAX_FRAMES,
                'dims': 124
            })
            total_processed += 1

        except Exception as e:
            print(f"\nFailed to process {filepath}: {e}")

    # Save CSV labels
    csv_path = os.path.join(output_dir, label_file)
    with open(csv_path, 'w', newline='', encoding='utf-8') as f:
        writer = csv.writer(f)
        writer.writerow(['mfcc_filename', 'name_label', 'voice_type',
                         'original_file', 'frames', 'dims'])
        for label in all_labels:
            writer.writerow([
                label['mfcc_filename'],
                label['name_label'],
                label['voice_type'],
                label['original_file'],
                label['frames'],
                label['dims']
            ])

    # Print report
    print(f"\n{'=' * 50}")
    print(f"Processing completed! Success: {total_processed}/{len(all_files)}")

    male = sum(1 for l in all_labels if 'Male' in l['voice_type'])
    female = sum(1 for l in all_labels if 'Female' in l['voice_type'])
    elderly = sum(1 for l in all_labels if 'Elderly' in l['voice_type'])
    print(f"Male: {male}, Female: {female}, Elderly: {elderly}")
    print(f"Output directory: {os.path.abspath(output_dir)}")

    return all_labels


# ==================== Usage ====================
if __name__ == '__main__':
    AUDIO_ROOT = "mixRunesUnity"
    OUTPUT_DIR = "mfcc_124d_gpu"
    LABEL_FILE = "labels_124d.csv"

    # Process 6000+ files with GPU, expected time: 5-10 minutes
    labels = process_folder_gpu(AUDIO_ROOT, OUTPUT_DIR, LABEL_FILE)

    print("\nValidation...")
    test = np.load(os.path.join(OUTPUT_DIR, labels[0]['mfcc_filename']))
    print(f"Shape: {test.shape}, Dtype: {test.dtype}")
    print(f"Value range: [{test.min():.2f}, {test.max():.2f}]")