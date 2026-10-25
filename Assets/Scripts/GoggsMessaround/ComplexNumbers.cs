using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Complex
{
    private double Real { get; set; }
    private double Imaginary { get; set; }
    public float Re => (float)Real;
    public float Im => (float)Imaginary;
    public float Modulus => (float)GetModulus(this);
    public float Argument => (float)GetArgument(this);

    public static string ToString(Complex z)
    {
        return $"{z.Real}+{z.Imaginary}i";
    }

    public static double GetModulus(Complex z)
    {
        return Math.Sqrt(z.Imaginary * z.Imaginary + z.Real * z.Real);
    }

    public static double GetArgument(Complex z)
    {
        //double re = z.Real;
        //double im = z.Imaginary;
        //if (re >= 0 && im >= 0)
        //{
        //    if (im == 0)
        //    {
        //        return 0;
        //    }
        //    if (re == 0)
        //    {
        //        return Math.PI / 2;
        //    }
        //
        //    return Math.Atan(im / re);
        //}
        //else if (re <= 0 && im <= 0)
        //{
        //    if (im == 0)
        //    {
        //        return Math.PI;
        //    }
        //    if (re == 0)
        //    {
        //        return 3*Math.PI / 2;
        //    }
        //
        //    return Math.Atan(im / re) + Math.PI;
        //}else if (re <= 0 && im >= 0)
        //{
        //    if (im == 0)
        //    {
        //        return Math.PI;
        //    }
        //    if (re == 0)
        //    {
        //        return Math.PI / 2;
        //    }
        //
        //    return Math.PI - Math.Atan(-im / re);
        //}else if (re >= 0 && im <= 0)
        //{
        //    if (im == 0)
        //    {
        //        return 0;
        //    }
        //    if (re == 0)
        //    {
        //        return 3*Math.PI / 2;
        //    }
        //
        //    return Math.Tau - Math.Atan(-im / re);
        //}
        //
        //return Math.Atan(z.Imaginary/z.Real);
        return Math.Atan2(z.Imaginary, z.Real);
    }

    public static Complex Exp(Complex z)
    {
        //e^z,, e to the z power
        return new Complex
        {
            Real = Math.Exp(z.Real) * Math.Cos(z.Imaginary),
            Imaginary = Math.Exp(z.Real) * Math.Sin(z.Imaginary)
        };
    }

    public static Complex Sqrt(Complex z)
    {
        return z ^ ToComplex(0.5);
    }

    public static Complex Ln(Complex z)
    {
        double r = GetModulus(z);
        double t = GetArgument(z);
        return new Complex
        {
            Real = Math.Log(r),
            Imaginary = t
        };
    }

    public static Complex Ln(Complex z, int branch)
    {
        double r = GetModulus(z);
        double t = GetArgument(z);
        return new Complex
        {
            Real = Math.Log(r),
            Imaginary = t + branch * Math.PI * 2
        };
    }

    public static Complex Sin(Complex z)
    {
        double r = GetModulus(z);
        double t = GetArgument(z);
        return new Complex
        {
            Real = Math.Sin(r * Math.Cos(t)) * Math.Cosh(r * Math.Sin(t)),
            Imaginary = Math.Cos(r * Math.Cos(t)) * Math.Sinh(r * Math.Sin(t))
        };
    }

    public static Complex Cos(Complex z)
    {
        double r = GetModulus(z);
        double t = GetArgument(z);
        return new Complex
        {
            Real = Math.Cos(r * Math.Cos(t)) * Math.Cosh(r * Math.Sin(t)),
            Imaginary = Math.Sinh(r * Math.Sin(t)) * Math.Sin(r * Math.Cos(t))
        };
    }

    public static Complex Tan(Complex z)
    {
        return Sin(z) / Cos(z);
    }

    public static Complex Cot(Complex z)
    {
        return Cos(z) / Sin(z);
    }

    public static Complex Sec(Complex z)
    {
        return ToComplex(1) / Cos(z);
    }

    public static Complex Csc(Complex z)
    {
        return ToComplex(1) / Sin(z);
    }

    public static Complex Sinh(Complex z)
    {
        return ToComplex(0.5) * (Exp(z) - ToComplex(1) / Exp(z));
    }

    public static Complex Cosh(Complex z)
    {
        return ToComplex(0.5) * (Exp(z) + ToComplex(1) / Exp(z));
    }

    public static Complex Tanh(Complex z)
    {
        return (Exp(z) - ToComplex(1) / Exp(z)) / (Exp(z) + ToComplex(1) / Exp(z));
    }

    public static Complex Coth(Complex z)
    {
        return (Exp(z) + ToComplex(1) / Exp(z)) / (Exp(z) - ToComplex(1) / Exp(z));
    }

    public static Complex Sech(Complex z)
    {
        return ToComplex(1) / Cosh(z);
    }

    public static Complex Csch(Complex z)
    {
        return ToComplex(1) / Sinh(z);
    }

    public static Complex LogBase(Complex b, Complex z)
    {
        return Ln(z) / Ln(b);
    }

    public static Complex LogBase(double b, Complex z)
    {
        return Ln(z) / ToComplex(b);
    }

    public static double LogBase(double b, double x)
    {
        return Math.Log(x) / Math.Log(b);
    }

    public static Complex Log2(Complex z)
    {
        return LogBase(2, z);
    }

    public static Complex operator +(Complex z, Complex w)
    {
        return new Complex
        {
            Real = z.Real + w.Real,
            Imaginary = z.Imaginary + w.Imaginary
        };
    }

    public static Complex operator +(Complex z, double w)
    {
        return new Complex
        {
            Real = z.Real + w,
            Imaginary = z.Imaginary
        };
    }

    public static Complex operator +(Complex z, int w)
    {
        return new Complex
        {
            Real = z.Real + w,
            Imaginary = z.Imaginary
        };
    }

    public static Complex operator +(int z, Complex w)
    {
        return new Complex
        {
            Real = w.Real + z,
            Imaginary = w.Imaginary
        };
    }

    public static Complex operator +(double z, Complex w)
    {
        return new Complex
        {
            Real = w.Real + z,
            Imaginary = w.Imaginary
        };
    }

    public static Complex operator -(Complex z, Complex w)
    {
        return new Complex
        {
            Real = z.Real - w.Real,
            Imaginary = z.Imaginary - w.Imaginary
        };
    }

    public static Complex operator -(Complex z, double w)
    {
        return new Complex
        {
            Real = z.Real - w,
            Imaginary = z.Imaginary
        };
    }

    public static Complex operator -(Complex z, int w)
    {
        return new Complex
        {
            Real = z.Real - w,
            Imaginary = z.Imaginary
        };
    }

    public static Complex operator -(int z, Complex w)
    {
        return new Complex
        {
            Real = w.Real - z,
            Imaginary = w.Imaginary
        };
    }

    public static Complex operator -(double z, Complex w)
    {
        return new Complex
        {
            Real = w.Real - z,
            Imaginary = w.Imaginary
        };
    }

    public static Complex operator *(Complex z, Complex w)
    {
        return new Complex
        {
            Real = z.Real * w.Real - z.Imaginary * w.Imaginary,
            Imaginary = z.Imaginary * w.Real + z.Real + w.Imaginary
        };
    }

    public static Complex operator *(Complex z, double w)
    {
        return new Complex
        {
            Real = z.Real * w,
            Imaginary = z.Imaginary * w
        };
    }

    public static Complex operator *(double z, Complex w)
    {
        return new Complex
        {
            Real = w.Real * z,
            Imaginary = w.Imaginary * z
        };
    }

    public static Complex operator *(int z, Complex w)
    {
        return new Complex
        {
            Real = w.Real * z,
            Imaginary = w.Imaginary * z
        };
    }

    public static Complex operator *(Complex z, int w)
    {
        return new Complex
        {
            Real = z.Real * w,
            Imaginary = z.Imaginary * w
        };
    }

    public static Complex operator /(Complex z, Complex w)
    {
        double m = GetModulus(z);
        return new Complex
        {
            Real = (z.Real * w.Real + z.Imaginary * w.Imaginary) / m,
            Imaginary = (z.Imaginary * w.Real - z.Real * w.Imaginary) / m
        };
    }

    public static Complex operator /(Complex z, double w)
    {
        double m = GetModulus(z);
        return new Complex
        {
            Real = z.Real * w / m,
            Imaginary = z.Imaginary * w / m
        };
    }

    public static Complex operator /(Complex z, int w)
    {
        double m = GetModulus(z);
        return new Complex
        {
            Real = z.Real * w / m,
            Imaginary = z.Imaginary * w / m
        };
    }

    public static Complex operator /(int w, Complex z)
    {
        double m = GetModulus(z);
        return new Complex
        {
            Real = z.Real * w / m,
            Imaginary = z.Imaginary * w / m
        };
    }

    public static Complex operator /(double w, Complex z)
    {
        double m = GetModulus(z);
        return new Complex
        {
            Real = z.Real * w / m,
            Imaginary = z.Imaginary * w / m
        };
    }

    public static Complex operator ^(Complex z, Complex w)
    {
        double r = GetModulus(z);
        double t = GetArgument(z);
        Complex a = new Complex
        {
            Real = -t * w.Imaginary + w.Real * Math.Log(r),
            Imaginary = t * w.Real + w.Imaginary * Math.Log(r)
        };
        return Exp(a);
    }

    public static Complex operator ^(Complex z, double w)
    {
        double r = GetModulus(z);
        double t = GetArgument(z);
        Complex a = new Complex
        {
            Real = w * Math.Log(r),
            Imaginary = t * w
        };
        return Exp(a);
    }

    public static Complex operator ^(Complex z, int w)
    {
        double r = GetModulus(z);
        double t = GetArgument(z);
        Complex a = new Complex
        {
            Real = w * Math.Log(r),
            Imaginary = t * w
        };
        return Exp(a);
    }

    public static Complex operator ^(int w, Complex z)
    {
        double r = GetModulus(z);
        double t = GetArgument(z);
        Complex a = new Complex
        {
            Real = w * Math.Log(r),
            Imaginary = t * w
        };
        return Exp(a);
    }

    public static Complex operator ^(double w, Complex z)
    {
        double r = GetModulus(z);
        double t = GetArgument(z);
        Complex a = new Complex
        {
            Real = w * Math.Log(r),
            Imaginary = t * w
        };
        return Exp(a);
    }

    public Complex TryParse(string literal)
    {
        Complex complex = new Complex();
        string[] terms = literal.Split(new char[] { '+', '-' });
        char[] doubleChars =
        {
            '.',
            '0',
            '1',
            '2',
            '3',
            '4',
            '5',
            '6',
            '7',
            '8',
            '9',
            'e',
            'E'
        };
        string[] signs = literal.Split(doubleChars, StringSplitOptions.RemoveEmptyEntries);
        if (terms[0].Contains("i"))
        {
            terms[0] = terms[0].Replace("i", "");
            double.TryParse(signs[0] + terms[0], out double a);
            double.TryParse(signs[1] + terms[1], out double b);
            complex.Imaginary = a;
            complex.Real = b;
        }
        else if (terms[1].Contains("i"))
        {
            terms[1] = terms[1].Replace("i", "");
            double.TryParse(signs[1] + terms[1], out double a);
            double.TryParse(signs[0] + terms[0], out double b);
            complex.Imaginary = a;
            complex.Real = b;
        }

        return complex;
    }

    public static Complex ToComplex(double realA, double imaginaryB = 0)
    {
        return new Complex
        {
            Real = realA,
            Imaginary = imaginaryB
        };
    }

    public static Complex ToImaginary(double imaginary = 0)
    {
        return new Complex
        {
            Real = 0,
            Imaginary = imaginary
        };
    }

    public static Complex ToImaginary(Complex z)
    {
        return new Complex
        {
            Real = -z.Imaginary,
            Imaginary = z.Real
        };
    }

    public static Complex I(Complex z)
    {
        return ToImaginary(z);
    }

    public static Complex I(double d)
    {
        return ToImaginary(d);
    }

    public static Complex Conjugate(Complex z)
    {
        return new Complex
        {
            Real = z.Real,
            Imaginary = -z.Imaginary
        };
    }
}