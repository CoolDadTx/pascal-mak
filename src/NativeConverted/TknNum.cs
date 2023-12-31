﻿//  *************************************************************
//  *                                                           *
//  *   T O K E N S   (Numbers)                                 *
//  *                                                           *
//  *   Extract number tokens from the source file.             *
//  *                                                           *
//  *   CLASSES: TNumberToken,                                  *
//  *                                                           *
//  *   FILE:    prog3-2/tknnum.cpp                             *
//  *                                                           *
//  *   MODULE:  Scanner                                        *
//  *                                                           *
//  *   Copyright (c) 1996 by Ronald Mak                        *
//  *   For instructional purposes only.  No warranties.        *
//  *                                                           *
//  *************************************************************

//--------------------------------------------------------------
//  TNumberToken        Number token subclass of TToken.
//--------------------------------------------------------------
public class TNumberToken : TToken
{
    private char ch; // char fetched from input buffer
    private readonly StringBuilder ps = new StringBuilder(); // ptr into token string
    private int digitCount; // total no. of digits in number
    private bool countErrorFlag; // true if too many digits, else false

    //--------------------------------------------------------------
    //  AccumulateValue     Extract a number part from the source
    //                      and set its value.
    //
    //      pBuffer : ptr to text input buffer
    //      value   : accumulated value (from one or more calls)
    //      ec      : error code if failure
    //
    //  Return: true  if success
    //          false if failure
    //--------------------------------------------------------------
    public bool AccumulateValue ( TTextInBuffer buffer, ref float value, TErrorCode ec )
    {
        const int maxDigitCount = 20;

        //--Error if the first character is not a digit.
        if (Globals.charCodeMap[ch] != TCharCode.CcDigit)
        {
            Globals.Error(ec);
            return false; // failure
        }

        //--Accumulate the value as long as the total allowable
        //--number of digits has not been exceeded.
        do
        {
            ps.Append(ch);

            if (++digitCount <= maxDigitCount)
                value = 10 * value + (ch - '0'); // shift left and add
            else
                countErrorFlag = true; // too many digits

            ch = buffer.GetChar();
        } while (Globals.charCodeMap[ch] == TCharCode.CcDigit);

        return true; // success
    }

    public TNumberToken ()
    {
        code = TTokenCode.TcNumber;
    }

    //--------------------------------------------------------------
    //  Get         Extract a number token from the source and set
    //              its value.
    //
    //      pBuffer : ptr to text input buffer
    //--------------------------------------------------------------
    public override void Get ( TTextInBuffer buffer )
    {
        const int maxInteger = 32767;
        const int maxExponent = 37;

        float numValue = 0.0F; // value of number ignoring
                               //    the decimal point
        int wholePlaces = 0; // no. digits before the decimal point
        int decimalPlaces = 0; // no. digits after  the decimal point
        char exponentSign = '+';
        float eValue = 0.0F; // value of number after 'E'
        int exponent = 0; // final value of exponent
        var sawDotDotFlag = false; // true if encountered '..',
                                   //   else false

        ch = buffer.Char();
        
        digitCount = 0;
        countErrorFlag = false;
        code = TTokenCode.TcError; // we don't know what it is yet, but
        type = TDataType.TyInteger; //    assume it'll be an integer

        //--Get the whole part of the number by accumulating
        //--the values of its digits into numValue.  wholePlaces keeps
        //--track of the number of digits in this part.
        if (!AccumulateValue(buffer, ref numValue, TErrorCode.ErrInvalidNumber))
            return;
        wholePlaces = digitCount;

        //--If the current character is a dot, then either we have a
        //--fraction part or we are seeing the first character of a '..'
        //--token.  To find out, we must fetch the next character.
        if (ch == '.')
        {
            ch = buffer.GetChar();

            if (ch == '.')
            {

                //--We have a .. token.  Back up bufferp so that the
                //--token can be extracted next.
                sawDotDotFlag = true;
                buffer.PutBackChar();
            } else
            {
                type = TDataType.TyReal;
                ps.Append('.');

                //--We have a fraction part.  Accumulate it into numValue.
                if (!AccumulateValue(buffer, ref numValue, TErrorCode.ErrInvalidFraction))
                    return;
                decimalPlaces = digitCount - wholePlaces;
            }
        }

        //--Get the exponent part, if any. There cannot be an
        //--exponent part if we already saw the '..' token.
        if (!sawDotDotFlag && ((ch == 'E') || (ch == 'e')))
        {
            type = TDataType.TyReal;
            ps.Append(ch);
            ch = buffer.GetChar();

            //--Fetch the exponent's sign, if any.
            if ((ch == '+') || (ch == '-'))
            {
                exponentSign = ch;
                ps.Append(ch);                
                ch = buffer.GetChar();
            }

            //--Accumulate the value of the number after 'E' into eValue.
            digitCount = 0;
            if (!AccumulateValue(buffer, ref eValue, TErrorCode.ErrInvalidExponent))
                return;
            if (exponentSign == '-')
                eValue = -eValue;
        }

        //--Were there too many digits?
        if (countErrorFlag)
        {
            Globals.Error(TErrorCode.ErrTooManyDigits);
            return;
        }

        //--Calculate and check the final exponent value,
        //--and then use it to adjust the number's value.
        exponent = (int)eValue - decimalPlaces;
        if ((exponent + wholePlaces < -maxExponent) || (exponent + wholePlaces > maxExponent))
        {
            Globals.Error(TErrorCode.ErrRealOutOfRange);
            return;
        }
        if (exponent != 0)
            numValue *= (float)Math.Pow(10, exponent);

        //--Check and set the numeric value.
        if (type == TDataType.TyInteger)
        {
            if ((numValue < -maxInteger) || (numValue > maxInteger))
            {
                Globals.Error(TErrorCode.ErrIntegerOutOfRange);
                return;
            }
            value.integer = (int)numValue;
        } else
        {
            value.real = numValue;
        }

        this.String = ps.ToString();
        code = TTokenCode.TcNumber;
    }
    public override bool IsDelimiter () => false;

    //--------------------------------------------------------------
    //  Print       Print the token to the list file.
    //--------------------------------------------------------------
    public override void Print ()
    {
        if (type == TDataType.TyInteger)
            Globals.list.text = String.Format("\t{0,-18} ={1:D}", ">> integer:", value.integer);
        else
            Globals.list.text = String.Format("\t{0,-18} ={1:g}", ">> real:", value.real);

        Globals.list.PutLine();
    }
}
