using System;
using System.Diagnostics;
using System.Text;

namespace StrongInject.Generator;

internal class AutoIndenter
{
    public int Indent => _indent;
    
    public AutoIndenter(int initialIndent)
    {
        _indent = initialIndent;
        BeginLine();
    }
    
    private readonly StringBuilder _text = new();
    private int _indent;
    const string INDENT = "    ";
    public void Append(string str)
    {
        _text.Append(str);
    }

    public void Append(char c)
    {
        _text.Append(c);
    }
    
    public void AppendIndented(string str)
    {
        _text.Append(INDENT);
        _text.Append(str);
    }

    public void AppendLine(char c)
    {
        switch (c)
        {
            case '}':
                _indent--;
                _text.Remove(_text.Length - 5, 4);
                break;
            case '{':
                _indent++;
                break;
        }
        _text.Append(c);
        _text.AppendLine();
        BeginLine();
    }
    
    public void AppendLineIndented(string str)
    {
        _text.Append(INDENT);
        _text.AppendLine(str);
        BeginLine();
    }
    
    public void AppendLine(string str)
    {
        switch (str[0])
        {
            case '}':
                _indent--;
                _text.Remove(_text.Length - 4, 4);
                break;
            case '{':
                _indent++;
                break;
        }
        _text.AppendLine(str);
        BeginLine();
    }
    
    public void AppendLine()
    {
        _text.Insert(_text.Length - _indent * 4, Environment.NewLine);
    }

    private void BeginLine()
    {
        for (int i = 0; i < _indent; i++)
        {
            _text.Append(INDENT);
        }
    }

    public override string ToString()
    {
        return _text.ToString();
    }

    public AutoIndenter GetSubIndenter()
    {
        return new AutoIndenter(_indent);
    }

    public void Append(AutoIndenter subIndenter)
    {
        Debug.Assert(subIndenter._indent == _indent);
        _text.Remove(_text.Length - _indent * 4, _indent * 4);
        _text.Append(subIndenter._text);
    }
}