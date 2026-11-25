# ESPApp

Настольное приложение для работы с микроконтроллерами ESP32 через графический интерфейс.

## Технологии

- C#
- .NET
- Avalonia UI
- MVVM
- CommunityToolkit.Mvvm
- esptool

## Возможности

- Подключение через COM-порт.
- Определение модели и характеристик чипа.
- Чтение и запись Flash.
- Создание и восстановление резервных копий.
- Стирание областей Flash.
- Проверка записанных данных.
- Чтение и дамп памяти.
- Анализ BIN-образов.
- Конвертация ELF в BIN.
- Объединение BIN-файлов.
## Установка

Требования: .NET SDK, Python и esptool.

```
git clone https://github.com/class1cs/ESPApp.git
cd ESPApp
git checkout develop
dotnet restore
dotnet build
dotnet run
```

Установка esptool при необходимости:

```
python -m pip install esptool
```

## Безопасность
Перед записью или стиранием Flash создавайте резервную копию. Проверяйте адреса, размеры файлов и совместимость прошивки с чипом.
