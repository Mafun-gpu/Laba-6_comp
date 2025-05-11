using System;
using System.Collections.Generic;

namespace Laba_1
{
    public class NumberAutomaton
    {
        // внутренний класс состояния
        private class State
        {
            public Dictionary<Func<char, bool>, State> Transitions { get; }
                = new Dictionary<Func<char, bool>, State>();
            public bool IsAccepting { get; set; }
        }

        private readonly State _start;

        public NumberAutomaton()
        {
            // Строим граф автомата для шаблона: \b\d+(?:,\d+)?\b
            _start = new State();

            // Состояния: 0(start) → 1(целая часть) → [2(запятая)→3(дробная часть)]
            var s0 = _start;
            var s1 = new State { IsAccepting = true };   // после хотя бы одной цифры
            var s2 = new State { IsAccepting = false };  // после запятой
            var s3 = new State { IsAccepting = true };   // после хотя бы одной цифры дробной части

            // Переходы из s0 по цифре → s1
            s0.Transitions.Add(c => char.IsDigit(c), s1);
            // Из s1 по цифре остаёмся в s1
            s1.Transitions.Add(c => char.IsDigit(c), s1);
            // Из s1 по ',' → s2
            s1.Transitions.Add(c => c == ',', s2);
            // Из s2 по цифре → s3
            s2.Transitions.Add(c => char.IsDigit(c), s3);
            // Из s3 по цифре остаёмся в s3
            s3.Transitions.Add(c => char.IsDigit(c), s3);

            // Автомат построен: стартовое состояние s0,
            // допускающие: s1 (целое), s3 (дробное).
        }

        /// <summary>
        /// Находит все вхождения, возвращает список тройек (startIndex, length, value).
        /// </summary>
        public List<(int start, int length, string value)> FindMatches(string text)
        {
            var result = new List<(int, int, string)>();
            int n = text.Length;

            for (int i = 0; i < n; i++)
            {
                var state = _start;
                int lastAcceptPos = -1;
                int pos = i;

                // Итерируемся по символам от i
                while (pos < n)
                {
                    char c = text[pos];
                    bool moved = false;

                    foreach (var transition in state.Transitions)
                    {
                        if (transition.Key(c))
                        {
                            state = transition.Value;
                            moved = true;
                            pos++;

                            // Запоминаем позицию, если текущее состояние допускающее
                            if (state.IsAccepting)
                                lastAcceptPos = pos;
                            break;
                        }
                    }

                    if (!moved)
                        break;
                }

                // Если мы где-то встретили принимающее состояние — фиксируем максимально длинное
                if (lastAcceptPos > i)
                {
                    var len = lastAcceptPos - i;
                    var val = text.Substring(i, len);
                    result.Add((i, len, val));

                    // Пропускаем уже найденный участок
                    i = lastAcceptPos - 1;
                }
            }

            return result;
        }
    }
}
