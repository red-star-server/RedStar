## Strings for the "grant_connect_bypass" command.

cmd-grant_connect_bypass-desc = Временно позволяет пользователю обходить обычные проверки подключения.
cmd-grant_connect_bypass-help = Использование: grant_connect_bypass <user> [duration minutes]
    Временно позволяет пользователю обходить ограничения обычного подключения.
    Обход действует только на этом игровом сервере и истекает через 1 час по умолчанию.
    Пользователь сможет войти независимо от вайтлиста, аварийного бункера и лимита игроков.

cmd-grant_connect_bypass-arg-user = <user>
cmd-grant_connect_bypass-arg-duration = [duration minutes]

cmd-grant_connect_bypass-invalid-args = Ожидался 1 или 2 аргумента
cmd-grant_connect_bypass-unknown-user = Не удалось найти пользователя «{ $user }»
cmd-grant_connect_bypass-invalid-duration = Недопустимая длительность «{ $duration }»

cmd-grant_connect_bypass-success = Для пользователя «{ $user }» успешно добавлен обход проверок
