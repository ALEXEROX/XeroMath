using static System.Runtime.InteropServices.JavaScript.JSType;

public enum Sign { Plus = 1, Minus = -1 }

public class XeroNumber
{

    private readonly List<byte> digits = new List<byte>();

    public XeroNumber(List<byte> bytes)
    {
        digits = bytes.ToList();
        RemoveNulls();
    }

    public XeroNumber(Sign sign, List<byte> bytes)
    {
        Sign = sign;
        digits = bytes;
        RemoveNulls();
    }

    public XeroNumber(string s)
    {
        if (s.StartsWith("-"))
        {
            Sign = Sign.Minus;
            s = s.Substring(1);
        }

        foreach (var c in s.Reverse())
            digits.Add(Convert.ToByte(c.ToString()));

        RemoveNulls();
    }

    public XeroNumber(uint x) => digits.AddRange(GetBytes(x));

    public XeroNumber(int x)
    {
        if (x < 0)
            Sign = Sign.Minus;

        digits.AddRange(GetBytes((uint)Math.Abs(x)));
    }

    private List<byte> GetBytes(uint num)
    {
        List<byte> bytes = new List<byte>();
        do
        {
            bytes.Add((byte)(num % 10));
            num /= 10;
        } while (num > 0);

        return bytes;
    }

    private void RemoveNulls()
    {
        for (int i = digits.Count - 1; i > 0; i--)
        {
            if (digits[i] == 0)
                digits.RemoveAt(i);
            else
                break;
        }
    }

    public static XeroNumber Exp(byte val, int exp)
    {
        XeroNumber bigInt = Zero;
        bigInt.SetByte(exp, val);
        bigInt.RemoveNulls();
        return bigInt;
    }

    public static XeroNumber Zero => new XeroNumber(0);
    public static XeroNumber One => new XeroNumber(1);

    public int Size => digits.Count;

    public Sign Sign { get; private set; } = Sign.Plus;

    public byte GetByte(int i) => i < Size ? digits[i] : (byte)0;

    public void SetByte(int i, byte b)
    {
        while (digits.Count <= i)
            digits.Add(0);

        digits[i] = b;
    }

    public override string ToString()
    {
        if (this == Zero) return "0";
        string s = Sign == Sign.Plus ? "" : "-";

        for (int i = digits.Count - 1; i >= 0; i--)
            s += Convert.ToString(digits[i]);

        return s.ToString();
    }

    private static XeroNumber Add(XeroNumber a, XeroNumber b)
    {
        List<byte> digits = new List<byte>();
        int maxLength = Math.Max(a.Size, b.Size);
        byte t = 0;

        for (int i = 0; i < maxLength; i++)
        {
            byte sum = (byte)(a.GetByte(i) + b.GetByte(i) + t);
            if (sum > 10)
            {
                sum -= 10;
                t = 1;
            }
            else
                t = 0;

            digits.Add(sum);
        }

        if (t > 0)
            digits.Add(t);

        return new XeroNumber(a.Sign, digits);
    }

    private static XeroNumber Substract(XeroNumber a, XeroNumber b)
    {
        List<byte> digits = new List<byte>();

        XeroNumber max = Zero;
        XeroNumber min = Zero;

        int compare = Comparison(a, b, ignoreSign: true);

        switch (compare)
        {
            case -1:
                min = a;
                max = b;
                break;
            case 0:
                return Zero;
            case 1:
                min = b;
                max = a;
                break;
        }

        int maxLength = Math.Max(a.Size, b.Size);

        int t = 0;
        for (var i = 0; i < maxLength; i++)
        {
            int s = max.GetByte(i) - min.GetByte(i) - t;
            if (s < 0)
            {
                s += 10;
                t = 1;
            }
            else
                t = 0;

            digits.Add((byte)s);
        }

        return new XeroNumber(max.Sign, digits);
    }

    private static XeroNumber Multiply(XeroNumber a, XeroNumber b)
    {
        XeroNumber retValue = Zero;

        for (var i = 0; i < a.Size; i++)
        {
            for (int j = 0, carry = 0; (j < b.Size) || (carry > 0); j++)
            {
                int cur = retValue.GetByte(i + j) + a.GetByte(i) * b.GetByte(j) + carry;
                retValue.SetByte(i + j, (byte)(cur % 10));
                carry = cur / 10;
            }
        }

        retValue.Sign = a.Sign == b.Sign ? Sign.Plus : Sign.Minus;
        return retValue;
    }

    private static XeroNumber Div(XeroNumber a, XeroNumber b)
    {
        XeroNumber retValue = Zero;
        XeroNumber curValue = Zero;
        int x, l, r, m;
        XeroNumber cur, t;

        for (var i = a.Size - 1; i >= 0; i--)
        {
            curValue += Exp(a.GetByte(i), i);

            x = 0;
            l = 0;
            r = 10;
            while (l <= r)
            {
                m = (l + r) / 2;
                cur = b * Exp((byte)m, i);
                if (cur <= curValue)
                {
                    x = m;
                    l = m + 1;
                }
                else
                {
                    r = m - 1;
                }
            }

            retValue.SetByte(i, (byte)(x % 10));
            t = b * Exp((byte)x, i);
            curValue = curValue - t;
        }

        retValue.RemoveNulls();
        retValue.Sign = a.Sign == b.Sign ? Sign.Plus : Sign.Minus;

        return retValue;
    }

    private static XeroNumber Mod(XeroNumber a, XeroNumber b)
    {
        XeroNumber retValue = Zero;

        for (var i = a.Size - 1; i >= 0; i--)
        {
            retValue += Exp(a.GetByte(i), i);

            var x = 0;
            var l = 0;
            var r = 10;

            while (l <= r)
            {
                var m = (l + r) >> 1;
                var cur = b * Exp((byte)m, i);
                if (cur <= retValue)
                {
                    x = m;
                    l = m + 1;
                }
                else
                {
                    r = m - 1;
                }
            }

            retValue -= b * Exp((byte)x, i);
        }

        retValue.RemoveNulls();

        retValue.Sign = a.Sign == b.Sign ? Sign.Plus : Sign.Minus;
        return retValue;
    }

    private static int Comparison(XeroNumber a, XeroNumber b, bool ignoreSign = false)
    {
        return CompareSign(a, b, ignoreSign);
    }

    private static int CompareSign(XeroNumber a, XeroNumber b, bool ignoreSign = false)
    {
        if (!ignoreSign)
        {
            if (a.Sign < b.Sign)
            {
                return -1;
            }
            else if (a.Sign > b.Sign)
            {
                return 1;
            }
        }

        return CompareSize(a, b);
    }

    private static int CompareSize(XeroNumber a, XeroNumber b)
    {
        if (a.Size < b.Size)
        {
            return -1;
        }
        else if (a.Size > b.Size)
        {
            return 1;
        }

        return CompareDigits(a, b);
    }

    private static int CompareDigits(XeroNumber a, XeroNumber b)
    {
        var maxLength = Math.Max(a.Size, b.Size);
        for (var i = maxLength; i >= 0; i--)
        {
            if (a.GetByte(i) < b.GetByte(i))
            {
                return -1;
            }
            else if (a.GetByte(i) > b.GetByte(i))
            {
                return 1;
            }
        }

        return 0;
    }

    public static XeroNumber operator -(XeroNumber a)
    {
        a.Sign = a.Sign == Sign.Plus ? Sign.Minus : Sign.Plus;
        return a;
    }

    public static XeroNumber operator +(XeroNumber a, XeroNumber b) => a.Sign == b.Sign
            ? Add(a, b)
            : Substract(a, b);

    public static XeroNumber operator -(XeroNumber a, XeroNumber b) => a + -b;

    public static XeroNumber operator *(XeroNumber a, XeroNumber b) => Multiply(a, b);

    public static XeroNumber operator /(XeroNumber a, XeroNumber b) => Div(a, b);

    public static XeroNumber operator %(XeroNumber a, XeroNumber b) => Mod(a, b);

    public static bool operator <(XeroNumber a, XeroNumber b) => Comparison(a, b) < 0;

    public static bool operator >(XeroNumber a, XeroNumber b) => Comparison(a, b) > 0;

    public static bool operator <=(XeroNumber a, XeroNumber b) => Comparison(a, b) <= 0;

    public static bool operator >=(XeroNumber a, XeroNumber b) => Comparison(a, b) >= 0;

    public static bool operator ==(XeroNumber a, XeroNumber b) => Comparison(a, b) == 0;

    public static bool operator !=(XeroNumber a, XeroNumber b) => Comparison(a, b) != 0;

    public override bool Equals(object obj) => !(obj is XeroNumber) ? false : this == (XeroNumber)obj;

    public static XeroNumber Degree(int place, int indicator)
    {
        return Degree(new XeroNumber(place), indicator);
    }

    public static XeroNumber Degree(XeroNumber place, int indicator)
    {
        for (int i = 0; i < indicator; i++)
            place *= place;

        return place;
    }

    //Факториал
    public static XeroNumber Factorial(int number)
    {
        if (number < 0)
            throw new Exception("Factorial of negative number is not defined");

        if (number == 0)
            return XeroNumber.Zero;

        XeroNumber result = XeroNumber.One;

        for (int i = number; i >= 1; i--)
        {
            result = result * new XeroNumber(i);
        }

        return result;
    }

    //Факториал со значением состояния
    public static XeroNumber Factorial(int number, ref float ready)
    {
        if (number < 0)
            throw new Exception("Factorial of negative number is not defined");

        if (number == 0)
            return XeroNumber.Zero;

        XeroNumber result = XeroNumber.One;

        for (int i = number; i >= 1; i--)
        {
            result = result * new XeroNumber(i);
            ready = (float)(number - i) / number;
        }
        ready = 1;

        return result;
    }

    //Произведение от first до second
    public static XeroNumber PsevdoFactorial(int first, int second)
    {

        int min = Math.Min(first, second);
        int max = Math.Max(first, second);
        XeroNumber result = XeroNumber.One;

        for (int i = min; i <= max; i++)
        {
            result = result * new XeroNumber(i);
        }


        return result;
    }

    //Произведение от first до second со значением состояния
    public static XeroNumber PsevdoFactorial(int first, int second, ref float ready)
    {

        int min = Math.Min(first, second);
        int max = Math.Max(first, second);
        XeroNumber result = XeroNumber.One;

        for (int i = max; i >= min; i--)
        {
            result = result * new XeroNumber(i);
            ready = (float)(max - i) / (max - min);
        }
        ready = 1;


        return result;
    }

    //Комюинация без повтора
    public static XeroNumber CombinationWithoutRepeat(int up, int down)
    {
        if (up > down)
            throw new Exception("Combination not defined");

        int min = Math.Min(up, down - up);
        int max = Math.Max(up, down - up);

        return PsevdoFactorial(max + 1, down) / Factorial(min);
    }

    //Комюинация без повтора со значением состояния
    public static XeroNumber CombinationWithoutRepeat(int up, int down, ref float ready)
    {
        if (up > down)
            throw new Exception("Combination not defined");

        int min = Math.Min(up, down - up);
        int max = Math.Max(up, down - up);
        XeroNumber psevdofactorial = new XeroNumber(min);
        XeroNumber factorial = One;

        for (int i = down; i >= max + 1; i--)
        {
            psevdofactorial = psevdofactorial * new XeroNumber(i);
            ready = (float)(down - i) / (down - max) / 3;
        }

        ready = (float)1 / 3;

        for (int i = min; i >= 1; i--)
        {
            factorial = factorial * new XeroNumber(i);
            ready = (float)(min - i) / min / 3 + ((float)1 / 3);
        }

        ready = (float)2 / 3;



        XeroNumber retValue = Zero;
        XeroNumber curValue = Zero;
        XeroNumber cur, t;

        int x, l, r, m;

        for (int i = psevdofactorial.Size - 1; i >= 0; i--)
        {
            curValue += Exp(psevdofactorial.GetByte(i), i);

            x = 0;
            l = 0;
            r = 10;

            while (l <= r)
            {
                m = (l + r) / 2;
                cur = factorial * Exp((byte)m, i);
                if (cur <= curValue)
                {
                    x = m;
                    l = m + 1;
                }
                else
                {
                    r = m - 1;
                }
            }

            retValue.SetByte(i, (byte)(x % 10));
            t = factorial * Exp((byte)x, i);
            curValue = curValue - t;

            ready = (float)(psevdofactorial.Size - i) / psevdofactorial.Size / 3 + ((float)2 / 3);
        }
        retValue.RemoveNulls();
        ready = 1;

        return retValue;
    }

    //Размещение без повтора
    public static XeroNumber ArrangementWithoutRepeat(int up, int down)
    {
        if (up > down)
            throw new Exception("Arrangement not defined");

        return PsevdoFactorial(down - up + 1, down);
    }

    //Размещение без повтора со значением состояния
    public static XeroNumber ArrangementWithoutRepeat(int up, int down, ref float ready)
    {
        if (up > down)
            throw new Exception("Arrangement not defined");

        XeroNumber result = One;
        ready = 0;

        for (int i = down; i >= down - up + 1; i--)
        {
            result *= new XeroNumber(i);
            ready = (float)(down - i + 1) / (up + 1);
        }

        ready = 1;

        return result;
    }

    //Размещение с повтором со значением состояния
    public static XeroNumber ArrangementWithRepeat(int up, int down)
    {
        return Degree(up, down);
    }

    //Размещение с повтором
    public static XeroNumber ArrangementWithRepeat(int up, int down, ref float ready)
    {
        XeroNumber result = One;
        ready = 0;

        for (int i = 0; i < down; i++)
        {
            result *= new XeroNumber(up);
            ready = (float)(i + 1) / down;
        }

        ready = 1;

        return result;
    }
}