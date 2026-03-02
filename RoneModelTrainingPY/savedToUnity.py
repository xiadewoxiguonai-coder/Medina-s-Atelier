import tensorflow as tf
from tensorflow.keras.models import load_model
import numpy as np
import os
import tempfile
import shutil

os.environ['TF_ENABLE_ONEDNN_OPTS'] = '0'
os.environ['CUDA_VISIBLE_DEVICES'] = '-1'  # Disable GPU to avoid compatibility issues


def method1_tf2onnx_direct(keras_model_path, onnx_output_path):
    """
    Method 1: Direct conversion (suitable for older TF versions)
    """
    try:
        import tf2onnx

        print("Trying tf2onnx direct conversion...")
        model = load_model(keras_model_path)

        # Build input specification
        input_shape = model.input_shape
        spec = (tf.TensorSpec(input_shape, tf.float32, name="input"),)

        model_proto, _ = tf2onnx.convert.from_keras(
            model,
            input_signature=spec,
            opset=11,
            output_path=onnx_output_path
        )

        print(f"Method 1 success: {onnx_output_path}")
        return True

    except Exception as e:
        print(f"Method 1 failed: {e}")
        return False


def method2_onnx_keras(keras_model_path, onnx_output_path):
    """
    Method 2: Using onnx-keras (specialized for TF2.x)
    """
    try:
        # Try installing onnx-keras first
        try:
            import onnx_keras
        except ImportError:
            print("Installing onnx-keras...")
            import subprocess
            subprocess.check_call(['pip', 'install', 'onnx-keras', '-q'])
            import onnx_keras

        print("Trying onnx-keras conversion...")
        from onnx_keras import convert_keras

        model = load_model(keras_model_path)
        onnx_model = convert_keras(model)

        import onnx
        onnx.save(onnx_model, onnx_output_path)

        print(f"Method 2 success: {onnx_output_path}")
        return True

    except Exception as e:
        print(f"Method 2 failed: {e}")
        return False


def method3_keras2onnx(keras_model_path, onnx_output_path):
    """
    Method 3: Using keras2onnx (Microsoft official)
    """
    try:
        try:
            import keras2onnx
        except ImportError:
            print("Installing keras2onnx...")
            import subprocess
            subprocess.check_call(['pip', 'install', 'keras2onnx', '-q'])
            import keras2onnx

        print("Trying keras2onnx conversion...")
        model = load_model(keras_model_path)

        # Convert
        onnx_model = keras2onnx.convert_keras(model, model.name)
        keras2onnx.save_model(onnx_model, onnx_output_path)

        print(f"Method 3 success: {onnx_output_path}")
        return True

    except Exception as e:
        print(f"Method 3 failed: {e}")
        return False


def method4_manual_export(keras_model_path, onnx_output_path):
    """
    Method 4: Manual ONNX construction (most reliable but complex)
    Build computation graph directly using onnx library
    """
    try:
        import onnx
        from onnx import helper, TensorProto, numpy_helper

        print("Trying manual ONNX construction...")
        model = load_model(keras_model_path)

        # Get weights
        weights = {}
        for layer in model.layers:
            w = layer.get_weights()
            if w:
                weights[layer.name] = w

        # Simplified processing here, only supports specific layer types
        # In actual projects, you should iterate through each layer and create corresponding ONNX nodes

        print(f"Method 4 requires manual implementation for your specific model")
        print(f"Model layers: {[l.name for l in model.layers]}")
        return False

    except Exception as e:
        print(f"Method 4 failed: {e}")
        return False


def method5_torch_bridge(keras_model_path, onnx_output_path):
    """
    Method 5: Keras -> PyTorch -> ONNX (most reliable alternative)
    Use pytorch2keras or manual weight conversion
    """
    try:
        try:
            import torch
            import torch.nn as nn
        except ImportError:
            print("Installing torch...")
            import subprocess
            subprocess.check_call(['pip', 'install', 'torch', '-q'])
            import torch
            import torch.nn as nn

        print("Trying PyTorch bridge conversion...")
        model = load_model(keras_model_path)

        # Manually build equivalent PyTorch model
        class RuneCNN(nn.Module):
            def __init__(self):
                super().__init__()
                self.conv1 = nn.Conv2d(1, 32, 3, padding=1)
                self.pool1 = nn.MaxPool2d(2)
                self.dropout1 = nn.Dropout(0.25)

                self.conv2 = nn.Conv2d(32, 64, 3, padding=1)
                self.pool2 = nn.MaxPool2d(2)
                self.dropout2 = nn.Dropout(0.25)

                # Calculate flattened size
                # Input: [1, 100, 124, 1] -> conv1 -> [1, 100, 124, 32] -> pool -> [1, 50, 62, 32]
                # -> conv2 -> [1, 50, 62, 64] -> pool -> [1, 25, 31, 64]
                # Flatten: 25 * 31 * 64 = 49600
                self.flatten_size = 25 * 31 * 64

                self.fc1 = nn.Linear(self.flatten_size, 128)
                self.dropout3 = nn.Dropout(0.5)
                self.fc2 = nn.Linear(128, 24)

            def forward(self, x):
                # Adjust dimensions: [B, H, W, C] -> [B, C, H, W]
                x = x.permute(0, 3, 1, 2)

                x = torch.relu(self.conv1(x))
                x = self.pool1(x)
                x = self.dropout1(x)

                x = torch.relu(self.conv2(x))
                x = self.pool2(x)
                x = self.dropout2(x)

                x = x.reshape(x.size(0), -1)
                x = torch.relu(self.fc1(x))
                x = self.dropout3(x)
                x = self.fc2(x)
                return x

        # Create PyTorch model
        torch_model = RuneCNN()
        torch_model.eval()

        # Copy weights (manual mapping required)
        # This is a simplified example, actual implementation needs layer-by-layer copying
        print("Copying weights from Keras to PyTorch...")
        keras_weights = model.get_weights()

        # Manual weight mapping (adjust according to your model structure)
        with torch.no_grad():
            # Conv1: Keras [3,3,1,32] -> PyTorch [32,1,3,3]
            w = keras_weights[0]  # [3,3,1,32]
            w = np.transpose(w, (3, 2, 0, 1))  # [32,1,3,3]
            torch_model.conv1.weight.copy_(torch.FloatTensor(w))
            torch_model.conv1.bias.copy_(torch.FloatTensor(keras_weights[1]))

            # Conv2: Keras [3,3,32,64] -> PyTorch [64,32,3,3]
            w = keras_weights[2]  # [3,3,32,64]
            w = np.transpose(w, (3, 2, 0, 1))  # [64,32,3,3]
            torch_model.conv2.weight.copy_(torch.FloatTensor(w))
            torch_model.conv2.bias.copy_(torch.FloatTensor(keras_weights[3]))

            # Dense1
            w = keras_weights[4]
            w = w.T  # Keras uses [input, output], PyTorch uses [output, input]
            torch_model.fc1.weight.copy_(torch.FloatTensor(w))
            torch_model.fc1.bias.copy_(torch.FloatTensor(keras_weights[5]))

            # Dense2
            w = keras_weights[6]
            w = w.T
            torch_model.fc2.weight.copy_(torch.FloatTensor(w))
            torch_model.fc2.bias.copy_(torch.FloatTensor(keras_weights[7]))

        # Export to ONNX
        dummy_input = torch.randn(1, 100, 124, 1)

        torch.onnx.export(
            torch_model,
            dummy_input,
            onnx_output_path,
            export_params=True,
            opset_version=11,
            do_constant_folding=True,
            input_names=['input'],
            output_names=['output'],
            dynamic_axes={
                'input': {0: 'batch_size'},
                'output': {0: 'batch_size'}
            }
        )

        # Verification
        import onnx
        onnx_model = onnx.load(onnx_output_path)
        onnx.checker.check_model(onnx_model)

        print(f"Method 5 success: {onnx_output_path}")
        return True

    except Exception as e:
        print(f"Method 5 failed: {e}")
        import traceback
        traceback.print_exc()
        return False


def method6_simplified_onnx(keras_model_path, onnx_output_path):
    """
    Method 6: Using simplified ONNX export (optimized for Sequential models)
    """
    try:
        print("Trying simplified ONNX export...")
        model = load_model(keras_model_path)

        # Ensure model is called once to build graph
        import tensorflow as tf
        dummy_input = tf.zeros((1, 100, 124, 1))
        _ = model(dummy_input)

        # Save as SavedModel then convert with onnx
        with tempfile.TemporaryDirectory() as tmpdir:
            # Save as SavedModel
            tf.saved_model.save(model, tmpdir)

            # Convert using command line tool
            import subprocess
            result = subprocess.run([
                'python', '-m', 'tf2onnx.convert',
                '--saved-model', tmpdir,
                '--output', onnx_output_path,
                '--opset', '11'
            ], capture_output=True, text=True)

            if result.returncode == 0:
                print(f"Method 6 success: {onnx_output_path}")
                return True
            else:
                print(f"Method 6 failed: {result.stderr}")
                return False

    except Exception as e:
        print(f"Method 6 failed: {e}")
        return False


def verify_onnx(onnx_path):
    """Verify ONNX model"""
    try:
        import onnx
        import onnxruntime as ort

        model = onnx.load(onnx_path)
        onnx.checker.check_model(model)

        # Test inference
        session = ort.InferenceSession(onnx_path)
        input_name = session.get_inputs()[0].name
        output_name = session.get_outputs()[0].name

        test_input = np.random.randn(1, 100, 124, 1).astype(np.float32)
        output = session.run([output_name], {input_name: test_input})[0]

        print(f"ONNX verification passed!")
        print(f"  Input: {session.get_inputs()[0].shape}")
        print(f"  Output: {session.get_outputs()[0].shape}")
        print(f"  Test output: {output[0][:5]}")
        return True

    except Exception as e:
        print(f"ONNX verification failed: {e}")
        return False


def main():
    kear = "rune_ensemble_2"
    keras_model = kear + ".keras"

    methods = [
        ("tf2onnx direct", method1_tf2onnx_direct, "rune_method1.onnx"),
        ("onnx-keras", method2_onnx_keras, "rune_method2.onnx"),
        ("keras2onnx", method3_keras2onnx, "rune_method3.onnx"),
        ("manual export", method4_manual_export, "rune_method4.onnx"),
        ("PyTorch bridge", method5_torch_bridge, keras_model + ".onnx"),  # Most likely to succeed
        ("simplified", method6_simplified_onnx, "rune_method6.onnx"),
    ]

    success = False

    for name, method, output_path in methods:
        print(f"\n{'=' * 60}")
        print(f"Trying: {name}")
        print(f"{'=' * 60}")

        if method(keras_model, output_path):
            if verify_onnx(output_path):
                print(f"\nSUCCESS! ONNX model created: {output_path}")
                success = True

                # Rename to standard name
                if output_path != "rune_recognition_124D_48khz.onnx":
                    shutil.copy(output_path, kear + ".onnx")
                    print(f"Copied to: rune_recognition_124D_48khz.onnx")
                break

    if not success:
        print(f"\n{'=' * 60}")
        print("All ONNX methods failed.")
        print("Falling back to TFLite (which already works)...")
        print(f"{'=' * 60}")

        # Ensure TFLite exists
        if not os.path.exists("rune_recognition_124D_48khz.tflite"):
            print("Converting to TFLite...")
            # Call previous TFLite conversion code here
        else:
            print("TFLite model already exists: rune_recognition_124D_48khz.tflite")


if __name__ == "__main__":
    main()