cmd-align-desc =
    Автоматически выравнивает все закреплённые шлюзы, двери, пожарные шлюзы и т. д.
    относительно соседних конструкций.

    Используйте параметр [dry run], чтобы выполнить проверку без поворота сущностей.
cmd-align-help = Использование: { $command } [MapID] [dry run?]
cmd-align-no-release = Эта команда недоступна в сборке RELEASE.
cmd-align-hint-id = MapID
cmd-align-hint-dry = dry run?
cmd-align-feedback-none = {$dry ->
    [true] ПРОВЕРКА: Не
    *[false] Не
} найдены сущности, совместимые с AlignerSystem!
cmd-align-feedback-good = {$dry ->
    [true] ПРОВЕРКА: Не
    *[false] Не
} найдены неверно выровненные сущности.
cmd-align-feedback = {$dry ->
    [true] ПРОВЕРКА: Найдено
    *[false] Найдено и исправлено
} {$fixed ->
    [one] неверно выровненных сущностей: 1.
    *[else] неверно выровненных сущностей: { $fixed }.
}
