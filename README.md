# AdvancedFileViewer_WPF
Напишите программу интерпретатор и визуальный графический редактор для написания программ для вашего интерпретатора. 
Интерпретатор обрабатывает 32-х разрядные целые числа, в которых содержится код операции и номера регистров,
с которыми эта операция должна быть выполнена. Младшие 5 бит в числе — это код операции, затем следуют 9 битные номера 3-х регистров:

Возможные значения кода операции в 10-ой системе счисления:
0 – вывести состояние всех регистров в системе счисления, которая записана в 1 операнде;

1 – бинарное отрицание над содержимым 1 операнда, результат сохраняется в 3 операнд;

2 – дизъюнкция над 1 и 2 операндом, результат сохраняется в 3 операнд;

3 – конъюнкция над 1 и 2 операндом, результат сохраняется в 3 операнд;

4 – сложение по модулю 2 над 1 и 2 операндом, результат сохраняется в 3 операнд;

5 – импликация над 1 и 2 операндом, результат сохраняется в 3 операнд;

6 – коимпликация над 1 и 2 операндом, результат сохраняется в 3 операнд;

7 – эквиваленция над 1 и 2 операндом, результат сохраняется в 3 операнд;

8 – стрелка Пирса над 1 и 2 операндом, результат сохраняется в 3 операнд;

9 – штрих Шеффера над 1 и 2 операндом, результат сохраняется в 3 операнд;

10 – сложение 1 и 2 операнда, результат сохраняется в 3 операнд;

11 – вычитание из 1 операнда 2 операнда, результат сохраняется в 3 операнд;

12 – умножение 1 и 2 операнда, результат сохраняется в 3 операнд;

13 – целочисленное деление 1 операнда на 2 операнд, результат сохраняется в 3 операнд;

14 – остаток от деления 1 операнда на 2 операнд, результат сохраняется в 3 операнд

15 – обмен содержимого 1 и 2 операндов (операция swap);

16 – занести в 1 операнд в байт с номером, который находится во 2 операнде, байт, значение которое лежит на месте 3 операнда;

17 – вывести содержимое операнда 1 в системе счисления, которая записана месте для второго операнда;

18 – ввести в операнд 1 в системе счисления, которая записана месте для второго операнда значение с клавиатуры;

19 – найти максимальное значение 2𝑝, на которое делится 1 операнд, результат сохраняется в 3 операнд;

20 – сдвиг влево содержимого 1 операнда на количество бит, которое находится во 2-ом операнде, результат сохраняется в 3 операнд;

21 - сдвиг вправо содержимого 1 операнда на количество бит, которое находится во 2-ом операнде, результат сохраняется в 3 операнд;

22 – циклический сдвиг влево содержимого 1 операнда на количество бит, которое находится во 2-ом операнде, результат сохраняется в 3 операнд;

23 – циклический сдвиг вправо содержимого 1 операнда на количество бит, которое находится во 2-ом операнде, результат сохраняется в 3 операнд;

24 – занести в 1 операнд значение, которое стоит на месте 2 операнда.

Поток команд для интерпретатора поступает из двоичного потока. 
Визуальный графический редактор является классическим оконным приложением и позволяет конструировать инструкции для интерпретатора. 
В окне редактора слева присутствует область проектов, где в древовидном элементе управления отображаются проекты и файлы пользователя. 
Редактор содержит две рабочие области, в которых содержатся инструкции для интерпретатора в графическом и текстовом формате. 
Графическая область представляет собой последовательность визуализированных инструкций и операндов
(например, в каждой строке нарисованы прямоугольники, в которых написано название команды или номера регистров).
Пользователь может составлять инструкции просто выбирая соответствующие команды на панели инструментов. 
Текстовый формат содержит строковое представление команд (например, 1, 6, 3, xor).
При этом текстовый и графический формат должны быть согласованы. 
После окончания написания инструкций для интерпретатора пользователь должен иметь возможность запустить свою программу или сохранить ее.
Добавьте возможность отладки ваших программ.
Необходимо реализовать возможность пошагового выполнения инструкций с просмотром состояния задействованных переменных; 
добавьте возможность установки точек остановки (breakpoint).
