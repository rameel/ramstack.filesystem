# Code Review: Ramstack.FileSystem

## Обзор

Результаты полного code review библиотеки Ramstack.FileSystem.
Документ служит планом работ по исправлению найденных проблем.

---

## 1. Баги и проблемы в коде

### 1.1 Отсутствие проверки отмены в цикле `WriteAllLinesAsync`

**Файл:** `src/Ramstack.FileSystem.Abstractions/VirtualFileExtensions.cs`, строки 336–337
**Серьёзность:** Средняя

Метод `WriteAllLinesAsync` принимает `CancellationToken`, но не проверяет отмену внутри цикла `foreach`. Это несовместимо с `ReadAllLinesAsync` (строка 147), где `cancellationToken.ThrowIfCancellationRequested()` вызывается корректно.

**Сейчас:**
```csharp
foreach (var line in contents)
    await writer.WriteLineAsync(line).ConfigureAwait(false);
```

**Исправление:**
```csharp
foreach (var line in contents)
{
    cancellationToken.ThrowIfCancellationRequested();
    await writer.WriteLineAsync(line).ConfigureAwait(false);
}
```

---

### 1.2 Некорректная обработка отмены в `AsyncEnumeratorAdapter`

**Файл:** `src/Ramstack.FileSystem.Abstractions/Utilities/EnumerableExtensions.cs`, строка 51
**Серьёзность:** Средняя

`MoveNextAsync` при отмене возвращает `false` вместо выброса `OperationCanceledException`. Это маскирует отмену под нормальное завершение перечисления.

**Сейчас:**
```csharp
var result = !cancellationToken.IsCancellationRequested && enumerator.MoveNext();
return new ValueTask<bool>(result);
```

**Исправление:**
```csharp
cancellationToken.ThrowIfCancellationRequested();
return new ValueTask<bool>(enumerator.MoveNext());
```

---

### 1.3 Отсутствие проверки `null` в конструкторе `CompositeFileSystem`

**Файл:** `src/Ramstack.FileSystem.Composite/CompositeFileSystem.cs`, строки 36–37
**Серьёзность:** Средняя

Конструктор, принимающий `IEnumerable<IVirtualFileSystem>`, не проверяет аргумент на `null`, в отличие от `params`-перегрузки (строка 28).

**Сейчас:**
```csharp
public CompositeFileSystem(IEnumerable<IVirtualFileSystem> fileSystems) =>
    InternalFileSystems = fileSystems.ToArray();
```

**Исправление:**
```csharp
public CompositeFileSystem(IEnumerable<IVirtualFileSystem> fileSystems)
{
    ArgumentNullException.ThrowIfNull(fileSystems);
    InternalFileSystems = fileSystems.ToArray();
}
```

---

### 1.4 Отсутствие вызова `base.Dispose(disposing)` в `S3UploadStream`

**Файл:** `src/Ramstack.FileSystem.Amazon/S3UploadStream.cs`, строки 153–160
**Серьёзность:** Низкая (для `Stream` — no-op, но нарушение паттерна)

Метод `Dispose(bool)` делегирует работу `DisposeAsync()`, но не вызывает `base.Dispose(disposing)`.

**Исправление:**
```csharp
protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        using var scope = NullSynchronizationContext.CreateScope();
        DisposeAsync().AsTask().Wait();
    }

    base.Dispose(disposing);
}
```

---

## 2. Опечатки в XML-комментариях

### 2.1 Дублирование артикля "the"

**Файл:** `src/Ramstack.FileSystem.Abstractions/VirtualFileSystemExtensions.cs`, строка 192

**Сейчас:** `"Asynchronously writes the specified string to the specified the file."`
**Исправление:** `"Asynchronously writes the specified string to the specified file."`

---

### 2.2 Перепутан порядок слов

**Файл:** `src/Ramstack.FileSystem.Physical/PhysicalFileSystem.cs`, строка 24

**Сейчас:** `"The physical path of root the directory."`
**Исправление:** `"The physical path of the root directory."`

---

## 3. Несогласованности в XML-комментариях

### 3.1 Описание параметра `cancellationToken`

Основной вариант (подавляющее большинство): `"An optional cancellation token to cancel the operation."`

Нестандартные варианты, требующие унификации:

| Файл | Строка | Текущий текст |
|------|--------|--------------|
| `VirtualFileSystemExtensions.cs` | 27 | `"The optional cancellation token used for canceling the operation."` |
| `VirtualFileSystemExtensions.cs` | 40 | `"The optional cancellation token used for canceling the operation."` |
| `VirtualFileSystemExtensions.cs` | 276 | `"A cancellation token to cancel the operation."` |
| `VirtualFileSystemExtensions.cs` | 302 | `"An optional cancellation token to cancel the operation. Defaults to <see cref="CancellationToken.None"/>."` |
| `VirtualFileSystemExtensions.cs` | 316 | `"A token to cancel the operation. Defaults to <see cref="CancellationToken.None"/>."` |
| `VirtualFileSystemExtensions.cs` | 335 | `"A cancellation token to cancel the operation."` |
| `VirtualFileExtensions.cs` | 384 | `"An optional cancellation token to cancel the operation. Defaults to <see cref="CancellationToken.None"/>."` |
| `VirtualNode.cs` | 65 | `"A cancellation token to cancel the operation."` |
| `VirtualNode.cs` | 120 | `"A cancellation token to cancel the operation."` |
| `GcsFile.cs` | 145 | `"A cancellation token to cancel the operation."` |
| `AzureFile.cs` | 120 | `"A cancellation token to cancel the operation."` |
| `S3File.cs` | 137 | `"A cancellation token to cancel the operation."` |
| `S3UploadStream.cs` | 218 | `"A cancellation token to cancel the operation."` |
| `S3UploadStream.cs` | 272 | `"A cancellation token to cancel the operation."` |
| `AmazonS3FileSystem.cs` | 111 | `"A cancellation token to cancel the operation."` |
| `AmazonS3FileSystem.cs` | 122 | `"A cancellation token to cancel the operation."` |

**Рекомендация:** Унифицировать все до `"An optional cancellation token to cancel the operation."` — без суффикса `"Defaults to..."`.

---

### 3.2 Описание параметра `encoding`

Три разных варианта:

| Текст | Файлы и строки |
|-------|----------------|
| `"The character encoding to use."` | `VirtualFileExtensions.cs:29`, `VirtualFileSystemExtensions.cs:39` |
| `"The encoding applied to the contents."` | `VirtualFileExtensions.cs:69,133`, `VirtualFileSystemExtensions.cs:115,142` |
| `"The encoding to apply to the string."` | `VirtualFileExtensions.cs:272,297,326`, `VirtualFileSystemExtensions.cs:183,210,237` |

**Рекомендация:** Унифицировать до `"The encoding to use."` или разграничить: для чтения — `"The encoding applied to the contents."`, для записи — `"The encoding to apply to the string."`.

---

### 3.3 Использование "should" вместо "will" в remarks

**Файл:** `src/Ramstack.FileSystem.Abstractions/VirtualFile.cs`, строки 188–190

В `VirtualFile.WriteCoreAsync` remarks используют `"should"`:
```xml
<item><description>If the file does not exist, it should be created.</description></item>
<item><description>...the existing file should be overwritten.</description></item>
<item><description>...an exception should be thrown.</description></item>
```

В `VirtualFileSystemExtensions.cs` (строки 322–324) аналогичный remarks использует `"will"`:
```xml
<item><description>If the file does not exist, it will be created.</description></item>
```

**Рекомендация:** Для абстрактного метода (`WriteCoreAsync`) `"should"` может быть допустимо (предписание реализации), но для единообразия лучше использовать `"must"` или `"will"`.

---

### 3.4 Несогласованность `<returns>` для async-методов

**Файл:** `src/Ramstack.FileSystem.Abstractions/VirtualFileSystemExtensions.cs`, строки 29, 42

Используется `"A task representing the asynchronous operation and returns..."` — это не стандартный формат и не упоминает `ValueTask`/`Task` через `<see cref="..."/>`.

Остальные методы используют:
```xml
/// A <see cref="ValueTask"/> representing the asynchronous operation.
```

**Рекомендация:** Унифицировать до:
```xml
/// A <see cref="Task{TResult}"/> representing the asynchronous operation.
/// The task result contains a <see cref="StreamReader"/> that reads from the text file.
```

---

### 3.5 Описание параметра `file` в обёрточных классах

Разные формулировки для одного и того же:

| Файл | Текст |
|------|-------|
| `CompositeFile.cs:21` | `"The <see cref="VirtualFile"/> to wrap."` |
| `GlobbingFile.cs:21` | `"The <see cref="VirtualFile"/> instance to wrap."` |
| `ReadonlyFile.cs:18` | `"The <see cref="VirtualFile"/> instance to wrap."` |
| `PrefixedFile.cs:20` | `"The underlying <see cref="VirtualFile"/> that this instance wraps."` |
| `SubFile.cs:19` | `"The underlying <see cref="VirtualFile"/> instance to wrap."` |

**Рекомендация:** Унифицировать, например, до `"The <see cref="VirtualFile"/> instance to wrap."`.

---

### 3.6 Описание параметра `fs` в `VirtualFileSystemExtensions.cs`

Два варианта:

| Текст | Строки |
|-------|--------|
| `"The file system to use."` | Большинство методов |
| `"The <see cref="IVirtualFileSystem"/> instance."` | Строки 299, 312 (CopyFileAsync) |

**Рекомендация:** Унифицировать до `"The file system to use."`.

---

## 4. План работ

### Приоритет: Высокий
1. Исправить опечатку "the specified the file" (`VirtualFileSystemExtensions.cs:192`)
2. Исправить опечатку "root the directory" (`PhysicalFileSystem.cs:24`)
3. Добавить `cancellationToken.ThrowIfCancellationRequested()` в `WriteAllLinesAsync` (`VirtualFileExtensions.cs:336`)
4. Исправить `AsyncEnumeratorAdapter.MoveNextAsync` — бросать `OperationCanceledException` вместо возврата `false` (`EnumerableExtensions.cs:51`)
5. Добавить `ArgumentNullException.ThrowIfNull` в конструктор `CompositeFileSystem` (`CompositeFileSystem.cs:36`)

### Приоритет: Средний
6. Унифицировать описания `cancellationToken` (16 мест — см. раздел 3.1)
7. Унифицировать описания `encoding` (12 мест — см. раздел 3.2)
8. Исправить "should" → "will"/"must" в `VirtualFile.cs:188-190`
9. Унифицировать `<returns>` для `OpenTextAsync` (`VirtualFileSystemExtensions.cs:29,42`)
10. Унифицировать описания параметра `file` в обёрточных классах (5 мест — см. раздел 3.5)
11. Унифицировать описания параметра `fs` (`VirtualFileSystemExtensions.cs:299,312`)

### Приоритет: Низкий
12. Добавить `base.Dispose(disposing)` в `S3UploadStream.Dispose` (`S3UploadStream.cs:153`)
