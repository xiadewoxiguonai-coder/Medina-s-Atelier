import os
import sys
import re
from pathlib import Path
import numpy as np
from pedalboard import (
    Pedalboard, PitchShift, Reverb, LowpassFilter,
    HighpassFilter, Gain, Compressor, Chorus, Delay
)
from pedalboard.io import AudioFile


def sanitize_filename(name):
    """Clean filename, remove illegal characters"""
    # Remove or replace Windows/Unix illegal characters
    name = re.sub(r'[<>:"/\\|?*]', '', name)
    name = name.strip()
    # Replace spaces with underscores
    name = name.replace(' ', '_')
    return name


def process_female_voice(input_path, output_path, target_sr=48000):
    """
    Advanced female voice processing
    """
    try:
        with AudioFile(input_path) as f:
            audio = f.read(f.frames)
            samplerate = f.samplerate

        if len(audio.shape) > 1 and audio.shape[0] > 1:
            audio = np.mean(audio, axis=0, keepdims=True)

        board = Pedalboard([
            HighpassFilter(cutoff_frequency_hz=200),
            Compressor(threshold_db=-18, ratio=3, attack_ms=2, release_ms=50),
            PitchShift(semitones=7.0),
            Chorus(rate_hz=1.5, depth=0.2, centre_delay_ms=7, feedback=0.0, mix=0.15),
            Gain(gain_db=2.0),
            Reverb(room_size=0.15, damping=0.5, wet_level=0.08, dry_level=0.92, width=0.5),
            LowpassFilter(cutoff_frequency_hz=12000),
        ])

        processed = board(audio, samplerate)

        max_val = np.max(np.abs(processed))
        if max_val > 0.95:
            processed = processed * (0.95 / max_val)

        with AudioFile(output_path, 'w', samplerate, processed.shape[0], bit_depth=16) as f:
            f.write(processed)

        return True

    except Exception as e:
        print(f"    ✗ Female voice processing failed: {e}")
        return False


def process_elderly_voice(input_path, output_path, target_sr=48000):
    """
    Advanced elderly voice processing
    """
    try:
        with AudioFile(input_path) as f:
            audio = f.read(f.frames)
            samplerate = f.samplerate

        if len(audio.shape) > 1 and audio.shape[0] > 1:
            audio = np.mean(audio, axis=0, keepdims=True)

        board = Pedalboard([
            PitchShift(semitones=-3.5),
            Compressor(threshold_db=-20, ratio=2.5, attack_ms=5, release_ms=100),
            LowpassFilter(cutoff_frequency_hz=3500),
            Delay(delay_seconds=0.03, feedback=0.1, mix=0.1),
            Reverb(room_size=0.25, damping=0.7, wet_level=0.15, dry_level=0.85, width=0.3),
            Gain(gain_db=1.5),
            LowpassFilter(cutoff_frequency_hz=5000),
        ])

        processed = board(audio, samplerate)

        saturation_amount = 0.1
        processed = np.tanh(processed * (1 + saturation_amount)) / (1 + saturation_amount)

        max_val = np.max(np.abs(processed))
        if max_val > 0.95:
            processed = processed * (0.95 / max_val)

        with AudioFile(output_path, 'w', samplerate, processed.shape[0], bit_depth=16) as f:
            f.write(processed)

        return True

    except Exception as e:
        print(f"    ✗ Elderly voice processing failed: {e}")
        return False


def copy_original(input_path, output_path):
    """Copy original file (male voice)"""
    try:
        with AudioFile(input_path) as f:
            audio = f.read(f.frames)
            samplerate = f.samplerate

        with AudioFile(output_path, 'w', samplerate, audio.shape[0], bit_depth=16) as f:
            f.write(audio)

        return True
    except Exception as e:
        print(f"    ✗ Copy original failed: {e}")
        return False


def process_folder_structure(source_root, male_output_root, female_output_root, elderly_output_root):
    """
    Process entire folder structure, save with new naming convention
    Naming format: FolderName_Male_001.wav, FolderName_Female_001.wav, FolderName_Elderly_001.wav
    """
    source_path = Path(source_root)
    male_path = Path(male_output_root)
    female_path = Path(female_output_root)
    elderly_path = Path(elderly_output_root)

    # Create output root directories
    male_path.mkdir(parents=True, exist_ok=True)
    female_path.mkdir(parents=True, exist_ok=True)
    elderly_path.mkdir(parents=True, exist_ok=True)

    # Statistics
    total_files = 0
    male_success = 0
    female_success = 0
    elderly_success = 0
    failed_files = []

    # Get all subfolders
    subfolders = [f for f in sorted(source_path.iterdir()) if f.is_dir()]

    print(f"Found {len(subfolders)} subfolders")
    print("=" * 70)

    # Process all subfolders
    for idx, subfolder in enumerate(subfolders, 1):
        folder_name = subfolder.name
        # Clean folder name for filename
        clean_folder_name = sanitize_filename(folder_name)

        print(f"\n[{idx}/{len(subfolders)}] Processing folder: {folder_name}")
        print(f"  Clean name: {clean_folder_name}")

        # Get all wav files in this folder
        wav_files = sorted(subfolder.glob("*.wav"))

        if not wav_files:
            print(f"  Warning: No wav files in this folder")
            continue

        # Process each file with numbering
        for file_idx, wav_file in enumerate(wav_files, 1):
            total_files += 1
            file_number = f"{file_idx:03d}"  # 001, 002, 003...

            print(f"\n  File [{file_idx}/{len(wav_files)}]: {wav_file.name}")

            # ===== Male (original) =====
            male_filename = f"{clean_folder_name}_Male_{file_number}.wav"
            male_output = male_path / male_filename
            if copy_original(str(wav_file), str(male_output)):
                male_success += 1
                print(f"    ✓ Male: {male_filename}")
            else:
                failed_files.append((str(wav_file), "Male"))

            # ===== Female =====
            female_filename = f"{clean_folder_name}_Female_{file_number}.wav"
            female_output = female_path / female_filename
            if process_female_voice(str(wav_file), str(female_output)):
                female_success += 1
                print(f"    ✓ Female: {female_filename}")
            else:
                failed_files.append((str(wav_file), "Female"))

            # ===== Elderly =====
            elderly_filename = f"{clean_folder_name}_Elderly_{file_number}.wav"
            elderly_output = elderly_path / elderly_filename
            if process_elderly_voice(str(wav_file), str(elderly_output)):
                elderly_success += 1
                print(f"    ✓ Elderly: {elderly_filename}")
            else:
                failed_files.append((str(wav_file), "Elderly"))

    # Final report
    print("\n" + "=" * 70)
    print("Processing Complete!")
    print(f"Total source files: {total_files} (Generated {total_files * 3} files)")
    print(f"Male success:   {male_success}/{total_files} ({male_success / total_files * 100:.1f}%)")
    print(f"Female success: {female_success}/{total_files} ({female_success / total_files * 100:.1f}%)")
    print(f"Elderly success: {elderly_success}/{total_files} ({elderly_success / total_files * 100:.1f}%)")

    if failed_files:
        print(f"\nFailed files ({len(failed_files)}):")
        for file, vtype in failed_files[:10]:
            print(f"  - {vtype}: {file}")
        if len(failed_files) > 10:
            print(f"  ... and {len(failed_files) - 10} more")

    print("=" * 70)
    print(f"\nOutput directories:")
    print(f"  Male:    {male_output_root}")
    print(f"  Female:  {female_output_root}")
    print(f"  Elderly: {elderly_output_root}")


def main():
    # ==================== Configuration ====================

    # Option 1: Direct absolute path (recommended)
    # source_root = r"D:\UnityProjects\YourProject\Assets\RuneAudioRecords"

    # Option 2: Auto-detect
    current_dir = Path(__file__).parent.absolute()

    possible_paths = [
        current_dir / "Assets" / "RuneAudioRecords",
        current_dir / "RuneAudioRecords",
        current_dir.parent / "Assets" / "RuneAudioRecords",
        current_dir.parent / "RuneAudioRecords",
    ]

    source_root = None
    for path in possible_paths:
        if path.exists():
            source_root = str(path)
            print(f"✓ Auto-detected path: {source_root}")
            break

    if source_root is None:
        source_root = input("Please enter the full path to RuneAudioRecords folder: ").strip().strip('"')

    # Output directories (same level as source, flat structure)
    source_path = Path(source_root)
    parent_dir = source_path.parent

    male_output = str(parent_dir / "RuneAudioRecords_Male")
    female_output = str(parent_dir / "RuneAudioRecords_Female")
    elderly_output = str(parent_dir / "RuneAudioRecords_Elderly")

    # =================================================

    # Check source directory
    if not os.path.exists(source_root):
        print(f"✗ Error: Source directory does not exist: {source_root}")
        return

    print(f"\n{'=' * 70}")
    print(f"Source:      {source_root}")
    print(f"Male:        {male_output}")
    print(f"Female:      {female_output}")
    print(f"Elderly:     {elderly_output}")
    print(f"\nNaming format: FolderName_Male_001.wav")
    print(f"               FolderName_Female_001.wav")
    print(f"               FolderName_Elderly_001.wav")
    print(f"{'=' * 70}\n")

    # Confirm processing
    confirm = input("Confirm start processing? (y/n): ").strip().lower()
    if confirm != 'y':
        print("Cancelled")
        return

    # Start processing
    process_folder_structure(source_root, male_output, female_output, elderly_output)


if __name__ == "__main__":
    main()