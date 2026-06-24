import os
import sys
import zipfile
import datetime
import json
import argparse

# --- НАСТРОЙКИ ---
# --- планировщик задач windows: ---
# Создать простую задачу
# Имя: Surv Unity Project Daily Backup Script
# Описание: Автобэкап проекта Surv на Unity
# Действие: Запуск программы
# Программа или сценарий: python
# Добавить аргументы: "F:\NBNG\Surv\backup.py" --auto
# Рабочая папка: F:\NBNG\Surv\

CONFIG_FILE = "backup_py_config.json"

# Конфигурация по умолчанию (создается, если файла нет)
DEFAULT_CONFIG = {
    "backup_destination": r"F:\NBNG\Backups",
    
    # Полный бэкап: все важные папки и файлы проекта
    "full_sources": [
        r"F:\NBNG\Surv\Assets",
        r"F:\NBNG\Surv\Packages",
        r"F:\NBNG\Surv\ProjectSettings",
        r"F:\NBNG\Surv\UserSettings",
        r"F:\NBNG\Surv\Assembly-CSharp.csproj",
        r"F:\NBNG\Surv\Assembly-CSharp-Editor.csproj",
        r"F:\NBNG\Surv\.gitignore",
        r"F:\NBNG\Surv\backup.py",
        r"F:\NBNG\Surv\backup_py_config.json"
    ],

    # Быстрый бэкап: те же источники, но с исключениями
    "quick_sources": [
        r"F:\NBNG\Surv\Assets",
        r"F:\NBNG\Surv\Packages",
        r"F:\NBNG\Surv\ProjectSettings",
        r"F:\NBNG\Surv\UserSettings",
        r"F:\NBNG\Surv\Assembly-CSharp.csproj",
        r"F:\NBNG\Surv\Assembly-CSharp-Editor.csproj",
        r"F:\NBNG\Surv\.gitignore",
        r"F:\NBNG\Surv\backup.py",
        r"F:\NBNG\Surv\backup_py_config.json"
    ],

    # Исключения для БЫСТРОГО бэкапа. 
    # Указывайте имена папок ВНУТРИ sources, которые нужно пропустить.
    # Например, если в Assets есть папка "LargeTextures" или "Video", добавьте их сюда.
    # Также полезно исключать системные папки Unity, если они вдруг попали в sources:
    "quick_exclusions": [
        "_ExternalPackages",
        "_Models",
    ]
}

def load_config():
    """Загружает конфиг или создает дефолтный."""
    if not os.path.exists(CONFIG_FILE):
        print(f"Файл '{CONFIG_FILE}' не найден. Создаю конфигурацию по умолчанию...")
        with open(CONFIG_FILE, 'w', encoding='utf-8') as f:
            json.dump(DEFAULT_CONFIG, f, indent=4, ensure_ascii=False)
        print(f"Отредактируйте '{CONFIG_FILE}' под свои нужды и запустите скрипт снова.")
        sys.exit(0)
    
    with open(CONFIG_FILE, 'r', encoding='utf-8') as f:
        return json.load(f)

def create_backup_name(mode):
    """Генерирует имя: backup_mode.DD.MM.YY.HH.mm.zip"""
    now = datetime.datetime.now()
    # mode будет 'full' или 'quick'
    filename = f"backup_{mode}.{now.strftime('%d.%m.%y.%H.%M')}.zip"
    return filename

def is_excluded(file_path, exclusions):
    """Проверяет, находится ли файл в исключенной папке."""
    if not exclusions:
        return False
    
    # Нормализуем пути для сравнения
    norm_path = os.path.normpath(file_path)
    
    for excl in exclusions:
        # Проверяем, содержится ли имя исключенной папки в пути как отдельная директория
        # Пример: если excl = "Library", то путь ".../Assets/Library/file.txt" должен отсеяться
        if os.sep + excl + os.sep in norm_path or norm_path.endswith(os.sep + excl):
            return True
        # Также проверяем начало пути, если исключение - это корневая папка источника
        if norm_path.startswith(excl + os.sep) or norm_path == excl:
             # Это сработает, если мы передали полный путь к папке Library в exclusions.
             # Но у нас там только имена. Поэтому выше проверка через os.sep надежнее для вложенных папок.
             pass
             
    return False

def add_to_zip(zipf, source_path, base_dir, exclusions=None):
    """Рекурсивно добавляет файлы, учитывая исключения."""
    if not os.path.exists(source_path):
        print(f"[ПРЕДУПРЕЖДЕНИЕ] Путь не найден: {source_path}")
        return

    if os.path.isfile(source_path):
        # Проверка исключений для файлов
        if exclusions and is_excluded(source_path, exclusions):
            return # Пропускаем файл

        arcname = os.path.relpath(source_path, base_dir)
        try:
            zipf.write(source_path, arcname)
            # print(f"  + {arcname}") # Раскомментируйте для подробного лога
        except Exception as e:
            print(f"[ОШИБКА] Не удалось добавить {source_path}: {e}")

    elif os.path.isdir(source_path):
        # Если сама папка является исключением (например, мы явно указали папку Library в источниках)
        folder_name = os.path.basename(source_path)
        if exclusions and folder_name in exclusions:
            print(f"[ПРОПУСК] Папка исключена: {source_path}")
            return

        for root, dirs, files in os.walk(source_path):
            # Фильтрация папок на лету, чтобы os.walk не заходил внутрь исключенных директорий
            # Это сильно ускоряет работу
            if exclusions:
                dirs[:] = [d for d in dirs if d not in exclusions]
            
            for file in files:
                file_path = os.path.join(root, file)
                add_to_zip(zipf, file_path, base_dir, exclusions)

def run_backup(mode, config):
    """Основная логика бэкапа"""
    print(f"=== Запуск бэкапа: {mode.upper()} ===")
    
    dest_folder = config.get("backup_destination", "./backups")
    
    # Выбираем источники в зависимости от режима
    if mode == "full":
        sources = config.get("full_sources", [])
        exclusions = [] # В полном режиме ничего не исключаем (или можно добавить global_exclusions)
    else: # quick
        sources = config.get("quick_sources", [])
        exclusions = config.get("quick_exclusions", [])

    if not sources:
        print("Ошибка: Список источников пуст.")
        return

    # Подготовка папки
    if not os.path.exists(dest_folder):
        os.makedirs(dest_folder)
        print(f"Папка бэкапов создана: {dest_folder}")

    # Имя файла
    zip_filename = create_backup_name(mode)
    zip_filepath = os.path.join(dest_folder, zip_filename)
    print(f"Сохранение в: {zip_filepath}")

    start_time = time.time() if 'time' in globals() else None
    import time
    start_time = time.time()

    try:
        with zipfile.ZipFile(zip_filepath, 'w', zipfile.ZIP_DEFLATED) as zipf:
            for source in sources:
                source = source.strip()
                if source:
                    # Base dir для relpath: родитель файла или сама папка
                    base = os.path.dirname(source) if os.path.isfile(source) else os.path.dirname(source)
                    # Чтобы структура в архиве была относительно родителя источника.
                    # Например, источник F:\Proj\Assets -> в архиве будет Assets/...
                    
                    print(f"Обработка источника: {source}")
                    add_to_zip(zipf, source, base, exclusions)
        
        elapsed = time.time() - start_time
        size_mb = os.path.getsize(zip_filepath) / (1024 * 1024)
        
        print(f"\n=== УСПЕШНО ===")
        print(f"Архив: {zip_filename}")
        print(f"Размер: {size_mb:.2f} MB")
        print(f"Время выполнения: {elapsed:.2f} сек.")

    except Exception as e:
        print(f"=== КРИТИЧЕСКАЯ ОШИБКА ===")
        print(e)
        if os.path.exists(zip_filepath):
            os.remove(zip_filepath)

def main():
    # Парсинг аргументов командной строки
    parser = argparse.ArgumentParser(description="Скрипт резервного копирования")
    parser.add_argument('--mode', type=str, choices=['full', 'quick'], help="Тип бэкапа: full или quick")
    parser.add_argument('--auto', action='store_true', help="Автоматический режим (без вопросов, по умолчанию full)")
    args = parser.parse_args()

    config = load_config()

    selected_mode = "quick" # По умолчанию для ручного запуска

    if args.auto:
        # Для планировщика заданий всегда делаем полный бэкап (или можно настроить quick)
        selected_mode = "full" 
        print("Режим: АВТО (Полный бэкап)")
    elif args.mode:
        selected_mode = args.mode
        print(f"Режим: {selected_mode.upper()} (из аргументов)")
    else:
        # Интерактивный выбор
        print("\nВыберите тип бэкапа:")
        print("1. Быстрый (Quick) - без тяжелых папок [По умолчанию]")
        print("2. Полный (Full) - всё включено")
        
        choice = input("Ваш выбор (Enter для быстрого): ").strip().lower()
        
        if choice == '2' or choice == 'full':
            selected_mode = "full"
        else:
            selected_mode = "quick"
            
    run_backup(selected_mode, config)

if __name__ == "__main__":
    main()