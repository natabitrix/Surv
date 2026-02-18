import os
import shutil

# Укажи здесь свои пути 15:34
SOURCE_DIR = r"F:\NBNG\Xyark\Assets\Scripts\Core"      # ← замени на свой путь
# SOURCE_DIR = r"F:\NBNG\Xyark\Assets\Scripts\UI"      # ← замени на свой путь
# SOURCE_DIR = r"F:\NBNG\Xyark\Assets\Scripts\Crafting"      # ← замени на свой путь
# SOURCE_DIR = r"F:\NBNG\Xyark\Assets\Scripts\InventorySystem"      # ← замени на свой путь
# SOURCE_DIR = r"F:\NBNG\Xyark\Assets\Scripts\UI\Pausemenu"      # ← замени на свой путь
# Опционально: раскомментируй и укажи TARGET_DIR, если хочешь задать целевую папку вручную
# TARGET_DIR = r"C:\путь\к\другой\папке"

# Автоматическое определение целевой папки, если не задана вручную
if 'TARGET_DIR' not in locals() and 'TARGET_DIR' not in globals():
    parent_dir = os.path.dirname(SOURCE_DIR)
    source_folder_name = os.path.basename(SOURCE_DIR)
    TARGET_DIR = os.path.join(parent_dir, source_folder_name + "_txt")

def copy_and_rename_to_txt(source_dir, target_dir):
    if not os.path.isdir(source_dir):
        print(f"Ошибка: исходная папка '{source_dir}' не существует.")
        return

    os.makedirs(target_dir, exist_ok=True)

    for filename in os.listdir(source_dir):
        src_path = os.path.join(source_dir, filename)

        # Пропускаем папки и файлы с расширением .meta
        if os.path.isfile(src_path):
            if filename.lower().endswith('.meta'):
                # print(f"Пропущен: {filename} (файл .meta)")
                continue

            new_filename = filename + ".txt"
            dst_path = os.path.join(target_dir, new_filename)
            shutil.copy2(src_path, dst_path)
            print(f"Скопировано: {filename} → {new_filename}")

if __name__ == "__main__":
    print(f"Исходная папка: {SOURCE_DIR}")
    print(f"Целевая папка: {TARGET_DIR}")
    copy_and_rename_to_txt(SOURCE_DIR, TARGET_DIR)