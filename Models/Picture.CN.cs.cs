using System;
using System.Collections.Generic;
using System.Drawing.Imaging;

namespace Models
{
    // Alias -- od teraz zamiast pisać długiej nazwy Dictionary<Minutiae... można napisać Minutiaes
    using Minutiaes = Dictionary<MinutiaeType, List<(int X, int Y)>>;

    // public wskazuje, że ta klasa będzie widoczna spoza tego projektu, dzięki czemu projekt "Biometrics" korzysta z tej klasy
    // unsafe wskazuje, że będą wykorzystywane niezarządzane wskaźniki
    // partial wskazuje, że jest to częściowa implementacja klasy Picture -- to oznacza, że ta klasa została rozbita na wiele plików
    public unsafe partial class Picture
    {
        public Minutiaes CrossingNumber()
        {
            var minutiaes = new Minutiaes();

            // Dictionary<MinutiaeType, List<(int X, int Y)>> to słownik, 
            // w którym kluczami będą minucje typu MinutiaeType
            // a wartościami listy (wektory) cech
            // 
            // https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2?view=netframework-4.7.2

            BitmapData data = LockBits(ImageLockMode.ReadWrite);
            // Zablokowujemy obraz w pamięci dzięki czemu możemy na nim wykonywać operacje

            byte* ptr = (byte*)data.Scan0.ToPointer();
            // Wskaźnik na pierwszy element jednowymiarowej tablicy bajtów obrazu

            int stride = data.Stride; // Ilość bajtów w jednym wierszu obrazu; szerokość obrazu licząc bajtami
            int height = data.Height; // Wysokość obrazu

            for (int i = 1; i < height - 1; i++)
                for (int j = 3; j < stride - 3; j += 3)
                {
                    int offset = i * stride + j;

                    if (ptr[offset] == Zero)
                        // Interesują nas przypadki gdy środkowy piksel nie jest zerem
                        continue;

                    int sum = 0;

                    // Zliczanie sąsiadów gdy nie są zerami
                    if (IsValid(ptr[offset + 3]))
                        // Prawy środek
                        ++sum;

                    if (IsValid(ptr[offset - 3]))
                        // Lewy środek
                        ++sum;

                    for (int k = -1; k < 2; k++)
                    // Górny i dolny wiersz
                    {
                        if (IsValid(ptr[offset + stride + k * 3]))
                            ++sum;

                        if (IsValid(ptr[offset - stride + k * 3]))
                            ++sum;
                    }

                    switch (sum)
                    // Podejmujemy decyzję na podstawie ilości niezerowych sąsiadów
                    {
                        case 1: // Jeden niezerowy sąsiad - bieżący piksel to zakończenie linii papilarnych
                            Extract(minutiaes, ptr, i, j, offset, MinutiaeType.Ending);
                            break;

                        case 3: // Trzech niezerowych sąsiadów - bieżący piksel to rozgałęzienie linii papilarnych
                            Extract(minutiaes, ptr, i, j, offset, MinutiaeType.Bifurcation);
                            break;

                        case 4: // Czterech niezerowych sąsiadów - bieżący piksel to skrzyżowanie linii papilarnych
                            Extract(minutiaes, ptr, i, j, offset, MinutiaeType.Crossing);
                            break;

                        // Można zrobić więcej przypadków
                    }
                }

            UnlockBits(data);
            return minutiaes;
        }

        private static void Extract(
            Minutiaes minutiaes, 
            byte* ptr, 
            int i, 
            int j, 
            int offset, 
            MinutiaeType type
        )
        {
            // Odkomentowanie spowoduje pokolorowanie pikseli na których znaleziono którąkolwiek z cech
            //ptr[offset + 0] = 250;
            //ptr[offset + 1] = 120;
            //ptr[offset + 2] = 0;

            // Jeśli dana cecha już występuje
            if (minutiaes.ContainsKey(type))
                // To dodaj kolejny element do jej listy (wektora) cech
                minutiaes[type].Add((j / 3, i));
            else
                // W przeciwnym wypadku tworzy nową listę oraz dodaje do niej pierwsze wpółrzędne
                minutiaes[type] = new List<(int X, int Y)>() { (j / 3, i) };
        }

        public bool IsValid(byte b) => b != Zero;

        public static int GetDifferences(Minutiaes first, Minutiaes second)
        {
            // Tablica wszystkich możliwych wartości enuma MinutiaeType
            var values = (MinutiaeType[])Enum.GetValues(typeof(MinutiaeType));

            // Tablica częściowych różnic
            // Warto pamiętać, że ich wartości są domyślnie ustawione zero
            var count = new int[values.Length];

            for (int i = 0; i < first.Count; i++)
                // Ta metoda zwraca true jeśli dana cecha występuje, false w przeciwnym wypadku
                if (first.TryGetValue(values[i], out var list))
                    // Jeżeli dana lista (wektor) cech wystąpiła w pierwszym zestawie, to ustawiamy ilość cech na wartość początkową
                    count[i] = list.Count;

            for (int i = 0; i < first.Count; i++)
                if (second.TryGetValue(values[i], out var list))
                    // Odejmujemy ilość cech drugiego zestawu dzięki czemu po tej pętli dostaniemy różnicę długości list cech
                    count[i] -= list.Count;

            int sum = 0;
            foreach (int s in count)
                // Sumujemy wartości bezwzględne dzięki czemu dostaniemy ostateczny wynik
                sum += s > 0 ? s : -s;

            // Sumowanie wartości bezwzględnych jest konieczne 
            // Przykład działania:
            // Długości wektorów cech w pierwszym zestawie:  3  5  2
            // Długości wektorów cech w drugim    zestawie:  4  3  3
            //                        pierwszy minus drugi: -1  2 -1
            //
            // Interesuje nas różnica i dlatego dodajemy z pominięciem znaku, przez co wychodzi nam
            // |-1| + |2| + |-1| = 4
            //
            // Metoda w tym przypadku zwróci 4.

            return sum;
        }
    }
}
