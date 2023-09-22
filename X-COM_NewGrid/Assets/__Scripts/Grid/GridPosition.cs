


using System;

public struct GridPosition : IEquatable<GridPosition> //Интерфейс Equatable - Приравниваемый    //Наряду с классами структуры представляют еще один способ создания собственных типов данных в C#. Более того многие примитивные типы, например, int, double и т.д.,
                                                                                                //по сути являются структурами. При работе с структурой мы передаем копии значений а не ссылку.
                                                                                                //Мы создаем свою структуру т.к. не можем воспользоваться стандартным Vector2Int. Он работает с X Y а нам нужно X Z (можно было бы сделать преобразование ХУ в XZ туда и обратно, но это добовляет много строк кода и хуже воспринимается)
{
    public int x;
    public int z;
    public int floor;// Этаж на которой располагается наша сеточная позиция

    public GridPosition(int x, int z, int floor) // вспомогательный конструктор
    {
        this.x = x;
        this.z = z;
        this.floor = floor;
    }
    public override string ToString() // Переопределим ToString(). Хотим увидеть в отладке Debug.Log внутренее состояние X Z и этаж
    {
        return $"x: {x}; z: {z}; floor: {floor }" ;
    }



    public static bool operator == (GridPosition a, GridPosition b) // Расширение для булевых операций сравнения
    {
        return a.x==b.x && a.z==b.z && a.floor == b.floor ;
    }

    public static bool operator != (GridPosition a, GridPosition b) // Расширение для булевых операций сравнения
    {
        return !(a == b);
    }

    public static GridPosition operator + (GridPosition a, GridPosition b) // Расширение для суммы
    {
        return new GridPosition(a.x + b.x, a.z + b.z, a.floor + b.floor);
    }
    
    public static GridPosition operator - (GridPosition a, GridPosition b) // Расширение для разности
    {
        return new GridPosition(a.x - b.x, a.z - b.z, a.floor - b.floor);
    }

    public override bool Equals(object obj) // Автоматически сгенерированное расширение Переопределение равенства
    {
        return obj is GridPosition position &&
               x == position.x &&
               z == position.z &&
               floor == position.floor;
    }

    public override int GetHashCode() // Автоматически сгенерированное расширение
    {
        return HashCode.Combine(x, z, floor);
    }

    public bool Equals(GridPosition other) // Реализация интерфейса равенства
    {
        return this == other;
    }
}