# SecurityCheckApp

Windows-приложение (WinForms) для проверки базовых параметров защиты ПК:

1. Проверка подключения к интернету.
2. Проверка наличия межсетевого экрана.
3. Проверка работоспособности межсетевого экрана.
4. Проверка наличия антивируса.
5. Проверка работоспособности антивируса.
6. Вывод и сохранение результатов.

## Запуск в Windows (быстрый способ)

```bash
dotnet run --project SecurityCheckApp.csproj
```

## Как запустить финальный вариант как приложение на Windows

### Вариант 1. Через Visual Studio (рекомендуется)

1. Установите **Visual Studio 2022** с workload **.NET desktop development**.
2. Откройте проект `SecurityCheckApp.csproj`.
3. Нажмите **Build -> Build Solution**.
4. Запустите приложение кнопкой **Start** (или `Ctrl + F5`).

После сборки exe-файл будет в папке:

- `bin\Debug\net8.0-windows\SecurityCheckApp.exe` (Debug)
- `bin\Release\net8.0-windows\SecurityCheckApp.exe` (Release)

### Вариант 2. Через командную строку (готовый exe)

Откройте терминал в папке проекта и выполните:

```bash
dotnet publish SecurityCheckApp.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Готовый файл приложения будет в папке:

- `bin\Release\net8.0-windows\win-x64\publish\SecurityCheckApp.exe`

Этот `.exe` можно запускать как обычное приложение Windows.

## Примечание

Проект ориентирован только на Windows.

## Блок-схемы для пояснительной записки

См. файл `COURSEWORK_FLOWCHARTS.md`.
