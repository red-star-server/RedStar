command-help-usage =
    Использование:
command-help-invertible =
    Поведение этой команды можно инвертировать с помощью префикса «not».
command-description-tpto =
    Телепортирует указанные сущности к целевой сущности.
command-description-player-list =
    Возвращает список всех сессий игроков.
command-description-player-self =
    Возвращает текущую сессию игрока.
command-description-player-imm =
    Возвращает сессию игрока, указанного в аргументе.
command-description-player-entity =
    Возвращает сущности входных сессий.
command-description-self =
    Возвращает текущую присоединённую сущность.
command-description-physics-velocity =
    Возвращает скорость входных сущностей.
command-description-physics-angular-velocity =
    Возвращает угловую скорость входных сущностей.
command-description-buildinfo =
    Возвращает информацию о сборке игры.
command-description-cmd-list =
    Возвращает список всех команд для этой стороны.
command-description-explain =
    Объясняет указанное выражение, показывая описания и сигнатуры команд. Работает только с корректными выражениями и не может объяснить команды, которые не удалось разобрать.
command-description-search =
    Ищет указанное значение во входных данных.
command-description-stopwatch =
    Измеряет время выполнения указанного выражения.
command-description-types-consumers =
    Возвращает все команды, принимающие указанный тип.
command-description-types-tree =
    Отладочный инструмент, возвращающий все типы, к которым интерпретатор команд может привести входные данные.
command-description-types-gettype =
    Возвращает тип входных данных.
command-description-types-fullname =
    Возвращает полное имя входного типа согласно CoreCLR.
command-description-as =
    Приводит входные данные к указанному типу.
    Фактически это подсказка типа, если тип известен вам, но неизвестен интерпретатору.
command-description-count =
    Подсчитывает количество элементов во входных данных и возвращает целое число.
command-description-map =
    Применяет указанный блок к входным данным.
command-description-select =
    Выбирает из входных данных N объектов или N% объектов.
    Команду можно инвертировать с помощью not, чтобы выбрать всё, кроме N объектов.
command-description-comp =
    Возвращает указанный компонент входных сущностей, отбрасывая сущности без этого компонента.
command-description-delete =
    Удаляет входные сущности.
command-description-ent =
    Возвращает указанный идентификатор сущности.
command-description-entities =
    Возвращает все сущности на сервере.
command-description-paused =
    Фильтрует входные сущности по признаку приостановки.
command-description-with =
    Фильтрует входные сущности по наличию указанного компонента.
command-description-fuck =
    Выбрасывает исключение.
command-description-ecscomp-listty =
    Перечисляет все зарегистрированные типы компонентов.
command-description-cd =
    Изменяет текущий каталог сессии на указанный относительный или абсолютный путь.
command-description-ls-here =
    Перечисляет содержимое текущего каталога.
command-description-ls-in =
    Перечисляет содержимое указанного относительного или абсолютного пути.
command-description-methods-get =
    Возвращает все методы, связанные с входным типом.
command-description-methods-overrides =
    Возвращает все методы, переопределённые во входном типе.
command-description-methods-overridesfrom =
    Возвращает все методы указанного типа, переопределённые во входном типе.
command-description-cmd-moo =
    Задаёт важные вопросы.
command-description-cmd-descloc =
    Возвращает строку локализации описания команды.
command-description-cmd-getshim =
    Возвращает прослойку выполнения команды.
command-description-help =
    Кратко объясняет использование toolshed.
command-description-ioc-registered =
    Возвращает все типы, зарегистрированные в IoCManager текущего потока (обычно игрового потока).
command-description-ioc-get =
    Возвращает экземпляр регистрации IoC.
command-description-loc-tryloc =
    Пытается получить строку локализации и возвращает null при неудаче.
command-description-loc-loc =
    Получает строку локализации и возвращает нелокализованную строку при неудаче.
command-description-physics-angular_velocity =
    Возвращает угловую скорость указанных сущностей.
command-description-vars =
    Возвращает список всех переменных, заданных в этой сессии.
command-description-any =
    Возвращает true, если входные данные содержат хотя бы одно значение, иначе false.
command-description-contains =
    Проверяет, содержит ли входная последовательность указанное значение.
command-description-ArrowCommand =
    Присваивает входные данные переменной.
command-description-isempty =
    Возвращает true, если входные данные пусты, иначе false.
command-description-isnull =
    Возвращает true, если входное значение равно null, иначе false.
command-description-unique =
    Удаляет повторяющиеся значения из входной последовательности.
command-description-where =
    Принимает входную последовательность IEnumerable<T> и блок с сигнатурой T -> bool, определяющий, следует ли включать каждое значение в выходную последовательность.
command-description-do =
    Для обратной совместимости с BQL применяет указанные старые команды к входной последовательности.
command-description-named =
    Фильтрует входные сущности по имени с помощью регулярного выражения ^selector$.
command-description-prototyped =
    Фильтрует входные сущности по прототипу.
command-description-nearby =
    Создаёт список всех сущностей в указанном радиусе от входных сущностей.
command-description-first =
    Возвращает первый элемент указанной последовательности.
command-description-splat =
    Создаёт список из N копий блока, значения или переменной.
command-description-val =
    Приводит указанное значение, блок или переменную к указанному типу. В основном служит для обхода текущих ограничений переменных.
command-description-var =
    Возвращает содержимое указанной переменной, пытаясь автоматически определить её тип. В составных командах, изменяющих переменную, может потребоваться команда «val».
command-description-actor-controlled =
    Фильтрует сущности по наличию активного управления.
command-description-actor-session =
    Возвращает сессии, связанные с входными сущностями.
command-description-physics-parent =
    Возвращает родительские сущности входных сущностей.
command-description-emplace =
    Выполняет указанный блок для входных данных, помещая входное значение в переменную $value внутри блока.
    Для сущностей дополнительно создаёт переменные $wx, $wy, $proto, $desc, $name и $paused.
    Для других типов также могут создаваться дополнительные переменные; подробности приведены в документации соответствующего типа.
command-description-AddCommand =
    Выполняет числовое сложение.
command-description-SubtractCommand =
    Выполняет числовое вычитание.
command-description-MultiplyCommand =
    Выполняет числовое умножение.
command-description-DivideCommand =
    Выполняет числовое деление.
command-description-min =
    Возвращает меньшее из двух значений.
command-description-max =
    Возвращает большее из двух значений.
command-description-BitAndCommand =
    Выполняет побитовое И (AND).
command-description-bitor =
    Выполняет побитовое ИЛИ (OR).
command-description-BitXorCommand =
    Выполняет побитовое исключающее ИЛИ (XOR).
command-description-neg =
    Меняет знак входного значения.
command-description-GreaterThanCommand =
    Проверяет, больше ли первое значение второго: x > y.
command-description-LessThanCommand =
    Проверяет, меньше ли первое значение второго: x < y.
command-description-GreaterThanOrEqualCommand =
    Проверяет, больше ли первое значение второго или равно ему: x >= y.
command-description-LessThanOrEqualCommand =
    Проверяет, меньше ли первое значение второго или равно ему: x <= y.
command-description-EqualCommand =
    Сравнивает значения на равенство и возвращает true, если они равны.
command-description-NotEqualCommand =
    Сравнивает значения на неравенство и возвращает true, если они не равны.
command-description-append =
    Добавляет значение в конец входной последовательности.
command-description-DefaultIfNullCommand =
    Если входное значение равно null, заменяет его значением типа по умолчанию. Работает только с типами значений, а не с объектами.
command-description-OrValueCommand =
    Если входное значение равно null, использует указанное запасное значение.
command-description-DebugPrintCommand =
    Выводит указанное значение для отладки во время выполнения команды, передавая его дальше без изменений.
command-description-i =
    Целочисленная константа.
command-description-f =
    Константа типа float.
command-description-s =
    Строковая константа.
command-description-b =
    Логическая константа.
command-description-join =
    Объединяет две последовательности в одну.
command-description-reduce =
    Сворачивает последовательность в одно значение с помощью указанного блока.
    Левая часть блока передаётся неявно, а правая хранится в $value.
command-description-rep =
    Повторяет входное значение N раз, создавая последовательность.
command-description-take =
    Берёт N значений из входной последовательности.
command-description-spawn-at =
    Создаёт сущность в указанных координатах.
command-description-spawn-on =
    Создаёт сущность в координатах указанной сущности.
command-description-spawn-in =
    Создаёт сущность в указанном контейнере указанной сущности; если она не помещается, оставляет её в координатах этой сущности.
command-description-spawn-attached =
    Создаёт сущность, присоединённую к указанной сущности, в относительных координатах (0 0).
command-description-mappos =
    Возвращает координаты сущности относительно текущей карты.
command-description-pos =
    Возвращает координаты сущности.
command-description-tp-coords =
    Телепортирует указанные сущности в целевые координаты.
command-description-tp-to =
    Телепортирует указанные сущности к целевой сущности.
command-description-tp-into =
    Телепортирует указанные сущности внутрь целевой сущности, присоединяя их в относительных координатах (0 0).
command-description-comp-get =
    Возвращает указанный компонент указанной сущности.
command-description-comp-add =
    Добавляет указанный компонент указанной сущности.
command-description-comp-ensure =
    Обеспечивает наличие указанного компонента у указанной сущности.
command-description-comp-has =
    Проверяет наличие указанного компонента у указанной сущности.
command-description-AddVecCommand =
    Добавляет скаляр (одно значение) к каждому входному элементу.
command-description-SubVecCommand =
    Вычитает скаляр (одно значение) из каждого входного элемента.
command-description-MulVecCommand =
    Умножает каждый входной элемент на скаляр (одно значение).
command-description-DivVecCommand =
    Делит каждый входной элемент на скаляр (одно значение).
command-description-rng-to =
    Возвращает число между входным значением (включительно) и аргументом (не включительно).
command-description-rng-from =
    Возвращает число между аргументом (включительно) и входным значением (не включительно).
command-description-rng-prob =
    Возвращает логическое значение на основе входной вероятности (от 0 до 1).
command-description-sum =
    Вычисляет сумму входных значений.
command-description-bin =
    Группирует входные значения и подсчитывает число вхождений каждого уникального элемента.
command-description-extremes =
    Возвращает элементы с двух концов списка, чередуя их.
command-description-sortby =
    Сортирует входные данные по возрастанию вычисленного ключа.
command-description-sortmapby =
    Сортирует входные данные по возрастанию вычисленного ключа, затем заменяет каждое значение его ключом.
command-description-sort =
    Сортирует входные данные по возрастанию.
command-description-sortdownby =
    Сортирует входные данные по убыванию вычисленного ключа.
command-description-sortmapdownby =
    Сортирует входные данные по убыванию вычисленного ключа, затем заменяет каждое значение его ключом.
command-description-sortdown =
    Сортирует входные данные по убыванию.
command-description-iota =
    Возвращает список чисел от 1 до N.
command-description-to =
    Возвращает список чисел от N до M.
command-description-curtick =
    Текущий игровой тик.
command-description-curtime =
    Текущее игровое время (TimeSpan).
command-description-realtime =
    Реальное время с момента запуска (TimeSpan).
command-description-servertime =
    Текущее игровое время сервера или ноль при выполнении на сервере (TimeSpan).
command-description-replace =
    Заменяет входные сущности указанным прототипом, сохраняя только положение и поворот.
command-description-allcomps =
    Возвращает все компоненты указанной сущности.
command-description-entitysystemupdateorder-tick =
    Перечисляет порядок обновления систем сущностей на каждом тике.
command-description-entitysystemupdateorder-frame =
    Перечисляет порядок обновления систем сущностей на каждом кадре.
command-description-more =
    Выводит содержимое $more — дополнительные данные последней команды, которые Toolshed не вывел.
command-description-ModulusCommand =
    Вычисляет остаток от деления двух значений.
    Поведение зависит от типа; подробности приведены в документации C#.
command-description-ModVecCommand =
    Вычисляет остаток от деления каждого входного значения на указанную константу.
command-description-BitAndNotCommand =
    Выполняет побитовую операцию И-НЕ (AND-NOT) над входными данными.
command-description-bitornot =
    Выполняет побитовую операцию ИЛИ-НЕ (OR-NOT) над входными данными.
command-description-BitXnorCommand =
    Выполняет побитовую эквивалентность (XNOR) над входными данными.
command-description-BitNotCommand =
    Выполняет побитовую инверсию (NOT) входных данных.
command-description-abs =
    Вычисляет абсолютное значение входных данных (убирает знак).
command-description-average =
    Вычисляет среднее арифметическое входных значений.
command-description-bibytecount =
    Возвращает размер входного значения в байтах, если его тип реализует IBinaryInteger.
    Это НЕ sizeof.
command-description-shortestbitlength =
    Возвращает минимальное число битов, необходимое для представления входного значения.
command-description-countleadzeros =
    Подсчитывает число ведущих нулей в двоичном представлении входного значения.
command-description-counttrailingzeros =
    Подсчитывает число завершающих нулей в двоичном представлении входного значения.
command-description-fpi =
    Число пи (3.14159...) типа float.
command-description-fe =
    Число e (2.71828...) типа float.
command-description-ftau =
    Число тау (6.28318...) типа float.
command-description-fepsilon =
    Значение эпсилон для float: ровно 1.4e-45.
command-description-dpi =
    Число пи (3.14159...) типа double.
command-description-de =
    Число e (2.71828...) типа double.
command-description-dtau =
    Число тау (6.28318...) типа double.
command-description-depsilon =
    Значение эпсилон для double: ровно 4.9406564584124654E-324.
command-description-hpi =
    Число пи (3.14...) типа half.
command-description-he =
    Число e (2.71...) типа half.
command-description-htau =
    Число тау (6.28...) типа half.
command-description-hepsilon =
    Значение эпсилон для half: ровно 5.9604645E-08.
command-description-floor =
    Округляет входное значение вниз, к отрицательной бесконечности.
command-description-ceil =
    Округляет входное значение вверх, к положительной бесконечности.
command-description-round =
    Округляет входное значение.
command-description-trunc =
    Отбрасывает дробную часть входного значения.
command-description-round2frac =
    Округляет входное значение до указанного числа знаков после запятой.
command-description-exponentbytecount =
    Возвращает число байтов, необходимых для хранения показателя степени.
command-description-significandbytecount =
    Возвращает число байтов, необходимых для хранения мантиссы.
command-description-significandbitcount =
    Возвращает точную длину мантиссы в битах.
command-description-exponentshortestbitcount =
    Возвращает минимальное число битов для хранения показателя степени.
command-description-stepnext =
    Переходит к следующему представимому значению с плавающей точкой, увеличивая мантиссу на единицу с переносом.
command-description-stepprev =
    Переходит к предыдущему представимому значению с плавающей точкой, уменьшая мантиссу на единицу с заёмом.
command-description-checkedto =
    Преобразует входной числовой тип в целевой и выдаёт ошибку, если преобразование невозможно.
command-description-saturateto =
    Преобразует входной числовой тип в целевой, ограничивая значение границами допустимого диапазона.
    Например, при преобразовании 382 в byte результатом будет 255 — максимальное значение byte.
command-description-truncto =
    Преобразует входной числовой тип в целевой с усечением.
    Для целых чисел это побитовое приведение с расширением знака.
command-description-iscanonical =
    Проверяет, находится ли входное значение в канонической форме.
command-description-iscomplex =
    Проверяет, является ли входное значение комплексным числом (по значению, а не по типу).
command-description-iseven =
    Проверяет, является ли входное значение чётным.
    Это не пакет JavaScript.
command-description-isodd =
    Проверяет, является ли входное значение нечётным.
command-description-isfinite =
    Проверяет, является ли входное значение конечным.
command-description-isimaginary =
    Проверяет, является ли входное значение чисто мнимым (без действительной части).
command-description-isinfinite =
    Проверяет, является ли входное значение бесконечным.
command-description-isinteger =
    Проверяет, является ли входное значение целым числом (по значению, а не по типу).
command-description-isnan =
    Проверяет, является ли входное значение NaN («не число»).
    Это специальное значение с плавающей точкой; проверяется значение, а не тип.
command-description-isnegative =
    Проверяет, является ли входное значение отрицательным.
command-description-ispositive =
    Проверяет, является ли входное значение положительным.
command-description-isreal =
    Проверяет, является ли входное значение чисто действительным (без мнимой части).
command-description-issubnormal =
    Проверяет, является ли входное значение субнормальным.
command-description-iszero =
    Проверяет, равно ли входное значение нулю.
command-description-pow =
    Возводит левый аргумент в степень правого: x^y.
command-description-sqrt =
    Вычисляет квадратный корень входного значения.
command-description-cbrt =
    Вычисляет кубический корень входного значения.
command-description-root =
    Вычисляет корень N-й степени входного значения.
command-description-hypot =
    Вычисляет гипотенузу прямоугольного треугольника с катетами A и B.
command-description-sin =
    Вычисляет синус входного значения.
command-description-sinpi =
    Вычисляет синус входного значения, умноженного на пи.
command-description-asin =
    Вычисляет арксинус входного значения.
command-description-asinpi =
    Вычисляет арксинус входного значения, делённый на пи.
command-description-cos =
    Вычисляет косинус входного значения.
command-description-cospi =
    Вычисляет косинус входного значения, умноженного на пи.
command-description-acos =
    Вычисляет арккосинус входного значения.
command-description-acospi =
    Вычисляет арккосинус входного значения, делённый на пи.
command-description-tan =
    Вычисляет тангенс входного значения.
command-description-tanpi =
    Вычисляет тангенс входного значения, умноженного на пи.
command-description-atan =
    Вычисляет арктангенс входного значения.
command-description-atanpi =
    Вычисляет арктангенс входного значения, делённый на пи.
command-description-iterate =
    Применяет указанную функцию к входным данным N раз и возвращает список результатов.
    Функция последовательно применяется к значению, а все промежуточные значения сохраняются.
command-description-pick =
    Выбирает случайное значение из входных данных.
command-description-tee =
    Передаёт входные данные указанному блоку, игнорируя его результат.
    Позволяет создать ответвление кода и выполнить несколько операций над одним значением.
command-description-cmd-info =
    Возвращает CommandSpec указанной команды.
    При самостоятельном вызове выводит справку команды.
command-description-comp-rm =
    Удаляет указанный компонент из сущности.

command-description-overlay-toggle = Включает или выключает наложение
command-description-overlay-add = Добавляет наложение, если его ещё нет
command-description-overlay-remove = Удаляет наложение
