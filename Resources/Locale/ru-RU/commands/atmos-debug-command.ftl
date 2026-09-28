cmd-atvrange-desc = Задаёт диапазон отладки атмосферы (два числа с плавающей точкой: начало [красный] и конец [синий])
cmd-atvrange-help = Использование: { $command } <start> <end>
cmd-atvrange-error-start = Недопустимое число для начала диапазона
cmd-atvrange-error-end = Недопустимое число для конца диапазона
cmd-atvrange-error-zero = Масштаб не может быть нулевым: это приведёт к делению на ноль в AtmosDebugOverlay.

cmd-atvmode-desc = Задаёт режим отладки атмосферы. Масштаб автоматически сбрасывается.
cmd-atvmode-help = Использование: { $command } <TotalMoles/GasMoles/Temperature> [<gas ID (for GasMoles)>]
cmd-atvmode-error-invalid = Недопустимый режим
cmd-atvmode-error-target-gas = Для этого режима необходимо указать целевой газ.
cmd-atvmode-error-out-of-range = Не удалось разобрать идентификатор газа, либо он вне допустимого диапазона.
cmd-atvmode-error-info = Для этого режима не нужны дополнительные данные.

cmd-atvcbm-desc = Переключает красно-зелёно-синюю палитру на оттенки серого
cmd-atvcbm-help = Использование: { $command } <true/false>
cmd-atvcbm-error = Недопустимый флаг
