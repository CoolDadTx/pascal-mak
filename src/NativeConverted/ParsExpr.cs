﻿//  *************************************************************
//  *                                                           *
//  *   P A R S E R   (Expressions)                             *
//  *                                                           *
//  *   Parse expressions.                                      *
//  *                                                           *
//  *   CLASSES: TParser                                        *
//  *                                                           *
//  *   FILE:    prog8-1/parsexpr.cpp                           *
//  *                                                           *
//  *   MODULE:  Parser                                         *
//  *                                                           *
//  *   Copyright (c) 1996 by Ronald Mak                        *
//  *   For instructional purposes only.  No warranties.        *
//  *                                                           *
//  *************************************************************
partial class TParser
{
    //--------------------------------------------------------------
    //  ParseExpression     Parse an expression (binary relational
    //                      operators = < > <> <= and >= ).
    //
    //  Return: ptr to the expression's type object
    //--------------------------------------------------------------
    public TType ParseExpression ()
    {
        TType pResultType; // ptr to result type
        TType pOperandType; // ptr to operand type

        //--Parse the first simple expression.
        pResultType = ParseSimpleExpression();

        //--If we now see a relational operator,
        //--parse the second simple expression.
        if (Globals.TokenIn(token, Globals.tlRelOps))
        {
            GetTokenAppend();
            pOperandType = ParseSimpleExpression();

            //--Check the operand types and return the boolean type.
            TType.CheckRelOpOperands(pResultType, pOperandType);
            pResultType = Globals.pBooleanType;
        }

        //--Make sure the expression ended properly.
        Resync(Globals.tlExpressionFollow, Globals.tlStatementFollow, Globals.tlStatementStart);

        return pResultType;
    }

    //--------------------------------------------------------------
    //  ParseSimpleExpression       Parse a simple expression
    //                              (unary operators + or - , and
    //                              binary operators + - and OR).
    //
    //  Return: ptr to the simple expression's type object
    //--------------------------------------------------------------
    public TType ParseSimpleExpression ()
    {
        TType pResultType; // ptr to result type
        TType pOperandType; // ptr to operand type
        TTokenCode op; // operator
        var unaryOpFlag = false; // true if unary op, else false

        //--Unary + or -
        if (Globals.TokenIn(token, Globals.tlUnaryOps))
        {
            unaryOpFlag = true;
            GetTokenAppend();
        }

        //--Parse the first term.
        pResultType = ParseTerm();

        //--If there was a unary sign, check the term's type.
        if (unaryOpFlag)
            TType.CheckIntegerOrReal(pResultType);

        //--Loop to parse subsequent additive operators and terms.
        while (Globals.TokenIn(token, Globals.tlAddOps))
        {

            //--Remember the operator and parse the subsequent term.
            op = token;
            GetTokenAppend();
            pOperandType = ParseTerm();

            //--Check the operand types to determine the result type.
            switch (op)
            {

                case TTokenCode.TcPlus:
                case TTokenCode.TcMinus:

                //--integer <op> integer => integer
                if (TType.IntegerOperands(pResultType, pOperandType))
                    pResultType = Globals.pIntegerType;

                //--real    <op> real    => real
                //--real    <op> integer => real
                //--integer <op> real    => real
                else if (TType.RealOperands(pResultType, pOperandType))
                    pResultType = Globals.pRealType;

                else
                    Globals.Error(TErrorCode.ErrIncompatibleTypes);
                break;

                case TTokenCode.TcOR:

                //--boolean OR boolean => boolean
                TType.CheckBoolean(pResultType, pOperandType);
                pResultType = Globals.pBooleanType;
                break;
            }

        }

        return pResultType;
    }

    //--------------------------------------------------------------
    //  ParseTerm           Parse a term (binary operators * / DIV
    //                      MOD and AND).
    //
    //  Return: ptr to the term's type object
    //--------------------------------------------------------------
    public TType ParseTerm ()
    {
        TType pResultType; // ptr to result type
        TType pOperandType; // ptr to operand type
        TTokenCode op; // operator

        //--Parse the first factor.
        pResultType = ParseFactor();

        //--Loop to parse subsequent multiplicative operators and factors.
        while (Globals.TokenIn(token, Globals.tlMulOps))
        {

            //--Remember the operator and parse the subsequent factor.
            op = token;
            GetTokenAppend();
            pOperandType = ParseFactor();

            //--Check the operand types to determine the result type.
            switch (op)
            {

                case TTokenCode.TcStar:

                //--integer * integer => integer
                if (TType.IntegerOperands(pResultType, pOperandType))
                    pResultType = Globals.pIntegerType;

                //--real    * real    => real
                //--real    * integer => real
                //--integer * real    => real
                else if (TType.RealOperands(pResultType, pOperandType))
                    pResultType = Globals.pRealType;

                else
                    Globals.Error(TErrorCode.ErrIncompatibleTypes);
                break;

                case TTokenCode.TcSlash:

                //--integer / integer => real
                //--real    / real    => real
                //--real    / integer => real
                //--integer / real    => real
                if (TType.IntegerOperands(pResultType, pOperandType) || TType.RealOperands(pResultType, pOperandType))
                    pResultType = Globals.pRealType;
                else
                    Globals.Error(TErrorCode.ErrIncompatibleTypes);
                break;

                case TTokenCode.TcDIV:
                case TTokenCode.TcMOD:

                //--integer <op> integer => integer
                if (TType.IntegerOperands(pResultType, pOperandType))
                    pResultType = Globals.pIntegerType;
                else
                    Globals.Error(TErrorCode.ErrIncompatibleTypes);
                break;

                case TTokenCode.TcAND:

                //--boolean AND boolean => boolean
                TType.CheckBoolean(pResultType, pOperandType);
                pResultType = Globals.pBooleanType;
                break;
            }
        }

        return pResultType;
    }

    //--------------------------------------------------------------
    //  ParseFactor         Parse a factor (identifier, number,
    //                      string, NOT <factor>, or parenthesized
    //                      subexpression).
    //
    //  Return: ptr to the factor's type object
    //--------------------------------------------------------------
    public TType ParseFactor ()
    {
        TType pResultType; // ptr to result type

        switch (token)
        {

            //fig 8-19
            case TTokenCode.TcIdentifier:
            {

                //--Search for the identifier and enter it if
                //--necessary.  Append the symbol table node handle
                //--to the icode.
                TSymtabNode pNode = Find(pToken.String);
                icode.Put(pNode);

                if (pNode.defn.how == TDefnCode.DcUndefined)
                {
                    pNode.defn.how = TDefnCode.DcVariable;
                    TType.SetType(ref pNode.pType, Globals.pDummyType);
                }

                //--Based on how the identifier is defined,
                //--parse a constant, function call, or variable.
                switch (pNode.defn.how)
                {

                    case TDefnCode.DcFunction:
                    pResultType = ParseSubroutineCall(pNode, true);
                    break;

                    case TDefnCode.DcProcedure:
                    Globals.Error(TErrorCode.ErrInvalidIdentifierUsage);
                    pResultType = ParseSubroutineCall(pNode, false);
                    break;

                    case TDefnCode.DcConstant:
                    GetTokenAppend();
                    pResultType = pNode.pType;
                    break;

                    default:
                    pResultType = ParseVariable(pNode);
                    break;
                }

                break;
            }
            
            case TTokenCode.TcNumber:
            {

                //--Search for the number and enter it if necessary.
                TSymtabNode pNode = SearchAll(pToken.String);
                if (pNode == null)
                {
                    pNode = EnterLocal(pToken.String);

                    //--Determine the number's type, and set its value into
                    //--the symbol table node.
                    if (pToken.Type() == TDataType.TyInteger)
                    {
                        pResultType = Globals.pIntegerType;
                        pNode.defn.constant = ConstantDefn.FromInteger(pToken.Value().integer);
                    } else
                    {
                        pResultType = Globals.pRealType;
                        pNode.defn.constant = ConstantDefn.FromReal(pToken.Value().real);
                    }
                    TType.SetType(ref pNode.pType, pResultType);
                }

                //--Append the symbol table node handle to the icode.
                icode.Put(pNode);

                pResultType = pNode.pType;
                GetTokenAppend();
                break;
            }

            case TTokenCode.TcString:
            {

                //--Search for the string and enter it if necessary.
                var pString = pToken.String;
                TSymtabNode pNode = SearchAll(pString);
                if (pNode == null)
                {
                    pNode = EnterLocal(pString);
                    pString = pNode.String();

                    //--Compute the string length (without the quotes).
                    //--If the length is 1, the result type is character,
                    //--else create a new string type.
                    int length = pString.Length - 2;
                    pResultType = length == 1 ? Globals.pCharType : new TType(length);
                    TType.SetType(ref pNode.pType, pResultType);

                    //--Set the character value or string pointer into the
                    //--symbol table node.
                    if (length == 1)                        
                        pNode.defn.constant = ConstantDefn.FromCharacter(pString[1]);
                    else
                        pNode.defn.constant = ConstantDefn.FromString(pString);
                }

                //--Append the symbol table node handle to the icode.
                icode.Put(pNode);

                pResultType = pNode.pType;
                GetTokenAppend();
                break;
            }

            case TTokenCode.TcNOT:
            {
                //--The operand type must be boolean.
                GetTokenAppend();
                TType.CheckBoolean(ParseFactor());
                pResultType = Globals.pBooleanType;

                break;
            }

            case TTokenCode.TcLParen:
            {
                //--Parenthesized subexpression:  Call ParseExpression
                //--                              recursively ...
                GetTokenAppend();
                pResultType = ParseExpression();

                //-- ... and check for the closing right parenthesis.
                if (token == TTokenCode.TcRParen)
                    GetTokenAppend();
                else
                    Globals.Error(TErrorCode.ErrMissingRightParen);
                break;
            };

            default:
                Globals.Error(TErrorCode.ErrInvalidExpression);
                pResultType = Globals.pDummyType;

            break;
        }

        return pResultType;
    }

    //--------------------------------------------------------------
    //  ParseVariable       Parse a variable, which can be a simple
    //                      identifier, an array identifier followed
    //                      subscripts, or a record identifier
    //                      followed by  fields.
    //
    //      pId : ptr to the identifier's symbol table node
    //
    //  Return: ptr to the variable's type object
    //--------------------------------------------------------------
    public TType ParseVariable ( TSymtabNode pId )
    {
        TType pResultType = pId.pType; // ptr to result type

        //--Check how the variable identifier was defined.
        switch (pId.defn.how)
        {
            case TDefnCode.DcVariable:
            case TDefnCode.DcValueParm:
            case TDefnCode.DcVarParm:
            case TDefnCode.DcFunction:
            case TDefnCode.DcUndefined:
            break; // OK

            default:
            pResultType = Globals.pDummyType;
            Globals.Error(TErrorCode.ErrInvalidIdentifierUsage);
            break;
        }

        GetTokenAppend();

        //-- [ or . : Loop to parse any subscripts and fields.
        var doneFlag = false;
        do
        {
            switch (token)
            {

                case TTokenCode.TcLBracket:
                    pResultType = ParseSubscripts(pResultType);
                    break;

                case TTokenCode.TcPeriod:
                    pResultType = ParseField(pResultType);
                    break;

                default:
                    doneFlag = true;
                    break;
            }
        } while (!doneFlag);

        return pResultType;
    }

    //--------------------------------------------------------------
    //  ParseSubscripts     Parse a bracketed list of subscript
    //                      separated by commas, following an
    //                      array variable:
    //
    //                          [ <expr>, <expr>, ... ]
    //
    //      pType : ptr to the array's type object
    //
    //  Return: ptr to the array element's type object
    //--------------------------------------------------------------
    public TType ParseSubscripts ( TType pType )
    {
        //--Loop to parse a list of subscripts separated by commas.
        do
        {
            //-- [ (first) or , (subsequent)
            GetTokenAppend();

            //-- The current variable is an array type.
            if (pType.form == TFormCode.FcArray)
            {

                //--The subscript expression must be assignment type
                //--compatible with the corresponding subscript type.
                TType.CheckAssignmentTypeCompatible(pType.array.pIndexType, ParseExpression(), TErrorCode.ErrIncompatibleTypes);

                //--Update the variable's type.
                pType = pType.array.pElmtType;
            }

            //--No longer an array type, so too many subscripts.
            //--Parse the extra subscripts anyway for error recovery.
            else
            {
                Globals.Error(TErrorCode.ErrTooManySubscripts);
                ParseExpression();
            }

        } while (token == TTokenCode.TcComma);

        //-- ]
        CondGetTokenAppend(TTokenCode.TcRBracket, TErrorCode.ErrMissingRightBracket);

        return pType;
    }

    //--------------------------------------------------------------
    //  ParseField          Parse a field following a record
    //                      variable:
    //
    //                          . <id>
    //
    //      pType : ptr to the record's type object
    //
    //  Return: ptr to the field's type object
    //--------------------------------------------------------------
    public TType ParseField ( TType pType )
    {
        GetTokenAppend();

        if ((token == TTokenCode.TcIdentifier) && (pType.form == TFormCode.FcRecord))
        {
            TSymtabNode pFieldId = pType.record.pSymtab.Search(pToken.String);
            if (pFieldId == null)
                Globals.Error(TErrorCode.ErrInvalidField);
            icode.Put(pFieldId);

            GetTokenAppend();
            return pFieldId != null ? pFieldId.pType : Globals.pDummyType;
        } else
        {
            Globals.Error(TErrorCode.ErrInvalidField);
            GetTokenAppend();
            return Globals.pDummyType;
        }
    }
}