# Milestone 2 — SQLite, Entity Framework Core и CRUD

## Какво е CRUD

CRUD съкращава четирите основни операции над данни:

- **Create** — създаване на задача;
- **Read** — списък и преглед на задача;
- **Update** — редактиране и отчитане на напредък;
- **Delete** — изтриване след потвърждение.

В PlannerPro тези операции са в `TasksController` и използват асинхронните методи на Entity Framework Core.

## Как се движат данните

```text
HTML форма
→ model binding в TasksController
→ Data Annotations и ModelState
→ AppDbContext
→ EF Core SQL команда
→ plannerpro.db
→ redirect към списъка
```

При невалиден модел запис не се извършва и същият изглед показва грешките до съответните полета.

## Моделът TaskItem

`TaskItem` описва един ред от таблицата `Tasks`. Основните потребителски полета са заглавие, описание, категория, срок, прогнозни и изпълнени минути, важност и статус. Полетата `Priority`, `PriorityScore` и `PriorityReason` са системни и не се приемат от формата.

Атрибутите `[Required]`, `[StringLength]` и `[Range]` дават едновременно server-side правила и metadata за client-side валидацията. `IValidatableObject` проверява правилото, че изпълнените минути не могат да надвишат прогнозните.

## Защита от overposting

POST actions използват `[Bind(...)]` и после копират позволените полета към проследения entity. Така потребителят не може чрез променена HTTP заявка да зададе `PriorityScore`, `CreatedAtUtc` или друго системно поле.

Всички POST форми използват antiforgery token чрез ASP.NET Core Form Tag Helper и `[ValidateAntiForgeryToken]`.

## AppDbContext

`AppDbContext` е мостът между C# моделите и SQLite. `DbSet<TaskItem> Tasks` представя таблицата, а `OnModelCreating`:

- записва enum стойностите като четим текст;
- поставя индекси върху срок, статус и приоритет;
- запазва еднаква конфигурация за runtime и migrations.

## Migration

Migration `InitialCreate` съдържа инструкции за създаване на таблицата и индексите. При стартиране `DbInitializer` извиква `MigrateAsync()`, затова ново копие на проекта само изгражда базата.

`AppDbContextFactory` се използва единствено от `dotnet ef`. Тя позволява на EF инструментите да създадат контекста, без да стартират целия уеб сървър.

## Seed данни

Ако таблицата е празна, initializer-ът добавя 10 задачи с различни категории, срокове, статуси и важност. Seed-ът се изпълнява само веднъж и осигурява надеждна демонстрация.

## Защо използваме AsNoTracking

Списъкът и детайлният изглед само четат данни. `AsNoTracking()` казва на EF Core да не пази копие за бъдеща редакция, което намалява използваната памет. При Edit използваме проследен entity, защото трябва да запишем промени.

## Какво да обясним на защитата

1. Формата изпраща POST заявка към controller action.
2. Model binding създава `TaskItem` от полетата.
3. `ModelState` спира невалидния вход.
4. EF Core превежда LINQ и `SaveChangesAsync()` към SQL.
5. PRG моделът — POST/Redirect/GET — предотвратява повторно изпращане при refresh.
6. Migration описва схемата, а SQLite файлът съдържа конкретните данни.

## Проверки на този етап

- build: 0 грешки и 0 предупреждения;
- 6 автоматични теста;
- migration е приложена;
- 10 seed задачи са записани;
- Create и Edit са проверени през браузъра;
- невалидно заглавие показва клиентска грешка;
- основните GET маршрути връщат HTTP 200.
