import random

def generate_syllable_word(count: int, length_from: int, length_to: int, start: str, lang: str) -> list[str]:
    """
    Генерирует список слов с заданным началом и чередованием гласных и согласных.
    Если start='', то генерируется полностью случайное слово.
    
    :param count: Количество слов для генерации
    :param length_from: Минимальная длина слова
    :param length_to: Максимальная длина слова
    :param start: Начало слова (например, 'Ната'). Если '', то слово генерируется случайно.
    :param lang: Язык ('ru' или 'en')
    :return: Список сгенерированных слов
    """
    if lang == 'ru':
        vowels = 'аоуэеёиюя'  # гласные
        consonants = 'бвгджзйклмнпрстфхцчшщь'  # согласные
    else:
        vowels = 'aeiou'  # гласные
        consonants = 'bcdfghjklmnprstvwxyz'  # согласные

    result = []
    for _ in range(count):
        # Случайная длина слова в заданном диапазоне
        length = random.randint(length_from, length_to)

        if start == '':
            # Полностью случайное слово
            word = []
            # Определяем, с чего начать: с гласной или согласной
            is_vowel_first = random.choice([True, False])
            for i in range(length):
                if i == 0:  # Первая буква - заглавная, чередование не важно
                    if is_vowel_first:
                        letter = random.choice(vowels).upper()
                    else:
                        letter = random.choice(consonants).upper()
                else:
                    # Определяем, должна ли текущая буква быть гласной
                    is_current_vowel = is_vowel_first if (i % 2 == 0) else not is_vowel_first
                    if is_current_vowel:
                        letter = random.choice(vowels)
                    else:
                        letter = random.choice(consonants)
                word.append(letter)
        else:
            # Слово с заданным началом
            word = list(start)

            # Если длина начала >= длина слова — просто возвращаем начало
            if len(word) >= length:
                result.append(''.join(word[:length]))
                continue

            # Определяем, чем заканчивается начало: гласной или согласной
            last_char = word[-1].lower()
            is_last_vowel = last_char in vowels
            is_last_consonant = last_char in consonants

            if not (is_last_vowel or is_last_consonant):
                raise ValueError(f"Буква '{last_char}' не найдена среди гласных или согласных.")

            # После последней буквы начала начинаем чередование
            for i in range(len(word), length):
                index_in_continuation = i - len(start)
                is_current_vowel = is_last_vowel if (index_in_continuation % 2 == 1) else not is_last_vowel

                if is_current_vowel:
                    letter = random.choice(vowels)
                else:
                    letter = random.choice(consonants)
                word.append(letter)

        result.append(''.join(word))
    
    return result

# Пример использования
if __name__ == '__main__':
    words = generate_syllable_word(count=100, length_from=5, length_to=12, start='', lang='en')
    for word in words:
        print(word)