﻿using RuriLib.Functions.Conditions;
using RuriLib.Functions.Conversions;
using RuriLib.LS;
using RuriLib.Models;
using RuriLib.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows.Media;

namespace RuriLib
{
    /// <summary>
    /// The available utility groups.
    /// </summary>
    public enum UtilityGroup
    {
        /// <summary>The group of actions performed on list variables.</summary>
        List,

        /// <summary>The group of actions performed on single variables.</summary>
        Variable,

        /// <summary>The group of action to convert an encoded string.</summary>
        Conversion,

        /// <summary>The group of actions that interact with files.</summary>
        File
    }

    /// <summary>
    /// Actions executed on single variables.
    /// </summary>
    public enum VarAction
    {
        /// <summary>Splits a variable into a list given a separator.</summary>
        Split
    }

    /// <summary>
    /// Actions executed on list variables.
    /// </summary>
    public enum ListAction
    {
        /// <summary>Creates an empty list variable.</summary>
        Create,

        /// <summary>Calculates the length of a list.</summary>
        Length,

        /// <summary>Joins a list into a single string, separating the elements with a separator.</summary>
        Join,

        /// <summary>Sorts a list alphabetically, in ascending or descending order.</summary>
        Sort,

        /// <summary>Concatenates two lists into a longer list variable.</summary>
        Concat,

        /// <summary>Zips two lists into a new list variable where the elements are joined two by two.</summary>
        Zip,

        /// <summary>Maps two lists into a dictionary variable.</summary>
        Map,

        /// <summary>Adds an element to a list variable.</summary>
        Add,

        /// <summary>Removes an element from a list variable given its index.</summary>
        Remove,

        /// <summary>Removes one ore more elements from a list variable given their value.</summary>
        RemoveValues,

        /// <summary>Removes duplicate elements from a list variable, keeping only the first one.</summary>
        RemoveDuplicates,

        /// <summary>Picks a random element from a list variable.</summary>
        Random,

        /// <summary>Randomizes the order of elements in a list.</summary>
        Shuffle
    }

    /// <summary>
    /// Actions that can be performed on files.
    /// </summary>
    public enum FileAction
    {
        /// <summary>Reads a file to a single variable.</summary>
        Read,

        /// <summary>Reads a file to a list variable.</summary>
        ReadLines,

        /// <summary>Writes a single variable to a file.</summary>
        Write,

        /// <summary>Writes a list variable to a file.</summary>
        WriteLines,

        /// <summary>Appends a single variable to a file.</summary>
        Append,

        /// <summary>Appends a list variable to a file.</summary>
        AppendLines
    }

    /// <summary>
    /// A block that performs actions on variables, converts values and operates on files.
    /// </summary>
    public class BlockUtility : BlockBase
    {
        // General
        private UtilityGroup group = UtilityGroup.List;
        /// <summary>The utility group.</summary>
        public UtilityGroup Group { get { return group; } set { group = value; OnPropertyChanged(); } }

        private string variableName = "";
        /// <summary>The name of the output variable.</summary>
        public string VariableName { get { return variableName; } set { variableName = value; OnPropertyChanged(); } }

        private bool isCapture = false;
        /// <summary>Whether the output variable should be marked for Capture.</summary>
        public bool IsCapture { get { return isCapture; } set { isCapture = value; OnPropertyChanged(); } }

        private string inputString = "";
        /// <summary>The input to process.</summary>
        public string InputString { get { return inputString; } set { inputString = value; OnPropertyChanged(); } }

        // Lists
        private ListAction listAction = ListAction.Join;
        /// <summary>The action to be performed on a list variable.</summary>
        public ListAction ListAction { get { return listAction; } set { listAction = value; OnPropertyChanged(); } }

        private string listName = "";
        /// <summary>The name of the target list variable.</summary>
        public string ListName { get { return listName; } set { listName = value; OnPropertyChanged(); } }

        #region ListUtility Specific Variables
        private string separator = ","; // Join
        /// <summary>The separator for joining a list.</summary>
        public string Separator { get { return separator; } set { separator = value; OnPropertyChanged(); } }

        private bool ascending = true;
        /// <summary>Whether the sort should happen in ascending order.</summary>
        public bool Ascending { get { return ascending; } set { ascending = value; OnPropertyChanged(); } }

        private bool numeric = false;
        /// <summary>Whether a list is made of numeric values.</summary>
        public bool Numeric { get { return numeric; } set { numeric = value; OnPropertyChanged(); } }

        private string secondListName = ""; // Zip
        /// <summary>The name of the second list variable.</summary>
        public string SecondListName { get { return secondListName; } set { secondListName = value; OnPropertyChanged(); } }

        private string listItem = ""; // Add
        /// <summary>The list item to add.</summary>
        public string ListItem { get { return listItem; } set { listItem = value; OnPropertyChanged(); } }

        private string listIndex = "-1"; // Add (-1 = end, 0 = start)
        /// <summary>The list index where an item can be added/removed. -1 = end, 0 = start.</summary>
        public string ListIndex { get { return listIndex; } set { listIndex = value; OnPropertyChanged(); } }

        private Comparer listElementComparer = Comparer.EqualTo;
        /// <summary>The comparer to use when removing or modifying one or more elements of a list.</summary>
        public Comparer ListElementComparer { get { return listElementComparer; } set { listElementComparer = value; OnPropertyChanged(); } }

        private string listComparisonTerm = "";
        /// <summary>The string that elements in a list should be compared to.</summary>
        public string ListComparisonTerm { get { return listComparisonTerm; } set { listComparisonTerm = value; OnPropertyChanged(); } }
        #endregion

        // Variables
        private VarAction varAction = VarAction.Split;
        /// <summary>The action to be performed on a single variable.</summary>
        public VarAction VarAction { get { return varAction; } set { varAction = value; OnPropertyChanged(); } }

        private string varName = "";
        /// <summary>The name of the single variable.</summary>
        public string VarName { get { return varName; } set { varName = value; OnPropertyChanged(); } }

        #region Variables Specific
        private string splitSeparator = "";
        /// <summary>The separator to split a string into a list.</summary>
        public string SplitSeparator { get { return splitSeparator; } set { splitSeparator = value; OnPropertyChanged(); } }
        #endregion

        // Conversion
        private Encoding conversionFrom = Encoding.HEX;
        /// <summary>The encoding to convert from.</summary>
        public Encoding ConversionFrom { get { return conversionFrom; } set { conversionFrom = value; OnPropertyChanged(); } }

        private Encoding conversionTo = Encoding.BASE64;
        /// <summary>The encoding to convert to.</summary>
        public Encoding ConversionTo { get { return conversionTo; } set { conversionTo = value; OnPropertyChanged(); } }

        // Files
        private string filePath = "test.txt";
        /// <summary>The path to the file to read/write.</summary>
        public string FilePath { get { return filePath; } set { filePath = value; OnPropertyChanged(); } }

        private FileAction fileAction = FileAction.Read;
        /// <summary>The action to be performed on the file.</summary>
        public FileAction FileAction { get { return fileAction; } set { fileAction = value; OnPropertyChanged(); } }

        /// <summary>
        /// Creates a Utility block.
        /// </summary>
        public BlockUtility()
        {
            Label = "UTILITY";
        }

        /// <inheritdoc />
        public override BlockBase FromLS(string line)
        {
            // Trim the line
            var input = line.Trim();

            // Parse the label
            if (input.StartsWith("#"))
                Label = LineParser.ParseLabel(ref input);

            // Parse the function
            Group = (UtilityGroup)LineParser.ParseEnum(ref input, "Group", typeof(UtilityGroup));

            // Parse specific function parameters
            switch (Group)
            {
                case UtilityGroup.List:

                    ListName = LineParser.ParseLiteral(ref input, "List Name");
                    ListAction = (ListAction)LineParser.ParseEnum(ref input, "List Action", typeof(ListAction));

                    switch (ListAction)
                    {
                        case ListAction.Join:
                            Separator = LineParser.ParseLiteral(ref input, "Separator");
                            break;

                        case ListAction.Sort:
                            while (LineParser.Lookahead(ref input) == TokenType.Boolean)
                                LineParser.SetBool(ref input, this);
                            break;

                        case ListAction.Concat:
                        case ListAction.Zip:
                        case ListAction.Map:
                            SecondListName = LineParser.ParseLiteral(ref input, "Second List Name");
                            break;

                        case ListAction.Add:
                            ListItem = LineParser.ParseLiteral(ref input, "Item");
                            ListIndex = LineParser.ParseLiteral(ref input, "Index");
                            break;

                        case ListAction.Remove:
                            ListIndex = LineParser.ParseLiteral(ref input, "Index");
                            break;

                        case ListAction.RemoveValues:
                            ListElementComparer = LineParser.ParseEnum(ref input, "Comparer", typeof(Comparer));
                            ListComparisonTerm = LineParser.ParseLiteral(ref input, "Comparison Term");
                            break;
                    }
                    break;

                case UtilityGroup.Variable:
                    VarName = LineParser.ParseLiteral(ref input, "Var Name");
                    VarAction = (VarAction)LineParser.ParseEnum(ref input, "Var Action", typeof(VarAction));

                    switch (VarAction)
                    {
                        case VarAction.Split:
                            SplitSeparator = LineParser.ParseLiteral(ref input, "SplitSeparator");
                            break;
                    }
                    break;

                case UtilityGroup.Conversion:
                    ConversionFrom = (Encoding)LineParser.ParseEnum(ref input, "Conversion From", typeof(Encoding));
                    ConversionTo = (Encoding)LineParser.ParseEnum(ref input, "Conversion To", typeof(Encoding));
                    InputString = LineParser.ParseLiteral(ref input, "Input");
                    break;

                case UtilityGroup.File:
                    FilePath = LineParser.ParseLiteral(ref input, "File Name");
                    FileAction = (FileAction)LineParser.ParseEnum(ref input, "File Action", typeof(FileAction));

                    switch (FileAction)
                    {
                        case FileAction.Write:
                        case FileAction.WriteLines:
                        case FileAction.Append:
                        case FileAction.AppendLines:
                            InputString = LineParser.ParseLiteral(ref input, "Input String");
                            break;
                    }
                    break;
            }

            // Try to parse the arrow, otherwise just return the block as is with default var name and var / cap choice
            if (LineParser.ParseToken(ref input, TokenType.Arrow, false) == string.Empty)
                return this;

            // Parse the VAR / CAP
            try
            {
                var varType = LineParser.ParseToken(ref input, TokenType.Parameter, true);
                if (varType.ToUpper() == "VAR" || varType.ToUpper() == "CAP")
                    IsCapture = varType.ToUpper() == "CAP";
            }
            catch { throw new ArgumentException("Invalid or missing variable type"); }

            // Parse the variable/capture name
            try { VariableName = LineParser.ParseToken(ref input, TokenType.Literal, true); }
            catch { throw new ArgumentException("Variable name not specified"); }

            return this;
        }

        /// <inheritdoc />
        public override string ToLS(bool indent = true)
        {
            var writer = new BlockWriter(GetType(), indent, Disabled);
            writer                
                .Label(Label)
                .Token("UTILITY")
                .Token(Group);

            switch (Group)
            {
                case UtilityGroup.List:
                    writer
                        .Literal(ListName)
                        .Token(ListAction);

                    switch (ListAction)
                    {
                        case ListAction.Join:
                            writer
                                .Literal(Separator);
                            break;

                        case ListAction.Sort:
                            writer
                                .Boolean(Ascending, "Ascending")
                                .Boolean(Numeric, "Numeric");
                            break;

                        case ListAction.Concat:
                        case ListAction.Zip:
                        case ListAction.Map:
                            writer
                                .Literal(SecondListName);
                            break;

                        case ListAction.Add:
                            writer
                                .Literal(ListItem)
                                .Literal(ListIndex);
                            break;

                        case ListAction.Remove:
                            writer
                                .Literal(ListIndex);
                            break;

                        case ListAction.RemoveValues:
                            writer
                                .Token(ListElementComparer)
                                .Literal(ListComparisonTerm);
                            break;
                    }
                    break;

                case UtilityGroup.Variable:
                    writer
                        .Literal(VarName)
                        .Token(VarAction);

                    switch (VarAction)
                    {
                        case VarAction.Split:
                            writer
                                .Literal(SplitSeparator);
                            break;
                    }
                    break;

                case UtilityGroup.Conversion:
                    writer
                        .Token(ConversionFrom)
                        .Token(ConversionTo)
                        .Literal(InputString);
                    break;

                case UtilityGroup.File:
                    writer
                        .Literal(FilePath)
                        .Token(FileAction);

                    switch (FileAction)
                    {
                        case FileAction.Write:
                        case FileAction.WriteLines:
                        case FileAction.Append:
                        case FileAction.AppendLines:
                            writer
                                .Literal(InputString);
                            break;
                    }
                    break;
            }

            if (!writer.CheckDefault(VariableName, "VariableName"))
                writer
                    .Arrow()
                    .Token(IsCapture ? "CAP" : "VAR")
                    .Literal(VariableName);

            return writer.ToString();
        }

        /// <inheritdoc />
        public override void Process(BotData data)
        {
            base.Process(data);

            try
            {
                switch (group)
                {
                    case UtilityGroup.List:

                        var list = data.Variables.GetList(listName);
                        var list2 = data.Variables.GetList(secondListName);
                        var item = ReplaceValues(listItem, data);
                        var index = int.Parse(ReplaceValues(listIndex, data));
                        CVar variable = null;

                        switch (listAction)
                        {
                            case ListAction.Create:
                                data.Variables.Set(new CVar(variableName, new List<string>(), isCapture));
                                break;

                            case ListAction.Length:
                                data.Variables.Set(new CVar(variableName, list.Count().ToString(), isCapture));
                                break;

                            case ListAction.Join:
                                data.Variables.Set(new CVar(variableName, string.Join(separator, list), isCapture));
                                break;

                            case ListAction.Sort:
                                var sorted = list.Select(e => e).ToList(); // Clone the list so we don't edit the original one
                                if (Numeric)
                                {
                                    var nums = sorted.Select(e => double.Parse(e, CultureInfo.InvariantCulture)).ToList();
                                    nums.Sort();
                                    sorted = nums.Select(e => e.ToString()).ToList();
                                }
                                else
                                {
                                    sorted.Sort();
                                }                                
                                if (!Ascending) sorted.Reverse();
                                data.Variables.Set(new CVar(variableName, sorted, isCapture));
                                break;

                            case ListAction.Concat:
                                data.Variables.Set(new CVar(variableName, list.Concat(list2).ToList(), isCapture));
                                break;

                            case ListAction.Zip:
                                data.Variables.Set(new CVar(variableName, list.Zip(list2, (a, b) => a + b).ToList(), isCapture));
                                break;

                            case ListAction.Map:
                                data.Variables.Set(new CVar(variableName, list.Zip(list2, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v), isCapture));
                                break;

                            case ListAction.Add:
                                // TODO: Refactor this
                                variable = data.Variables.Get(listName, CVar.VarType.List);
                                if (variable == null) variable = data.GlobalVariables.Get(listName, CVar.VarType.List);
                                if (variable == null) break;
                                if (variable.Value.Count == 0) index = 0;
                                else if (index < 0) index += variable.Value.Count;
                                variable.Value.Insert(index, item);
                                break;

                            case ListAction.Remove:
                                // TODO: Refactor this
                                variable = data.Variables.Get(listName, CVar.VarType.List);
                                if (variable == null) variable = data.GlobalVariables.Get(listName, CVar.VarType.List);
                                if (variable == null) break;
                                if (variable.Value.Count == 0) index = 0;
                                else if (index < 0) index += variable.Value.Count;
                                variable.Value.RemoveAt(index);
                                break;

                            case ListAction.RemoveValues:
                                data.Variables.Set(new CVar(variableName, list.Where(l => !Condition.Verify(new KeycheckCondition
                                {
                                    Left = ReplaceValues(l, data),
                                    Comparer = ListElementComparer,
                                    Right = ListComparisonTerm
                                })).ToList(), isCapture));
                                break;

                            case ListAction.RemoveDuplicates:
                                data.Variables.Set(new CVar(variableName, list.Distinct().ToList(), isCapture));
                                break;

                            case ListAction.Random:
                                data.Variables.Set(new CVar(variableName, list[data.random.Next(list.Count)], isCapture));
                                break;

                            case ListAction.Shuffle:
                                // This makes a copy of the original list
                                var listCopy = list.ToArray().ToList();
                                listCopy.Shuffle(data.random);
                                data.Variables.Set(new CVar(variableName, listCopy, isCapture));
                                break;

                            default:
                                break;
                        }
                        data.Log(new LogEntry($"Executed action {listAction} on file {listName}", Colors.White));
                        break;

                    case UtilityGroup.Variable:
                        string single;
                        switch (varAction)
                        {
                            case VarAction.Split:
                                single = data.Variables.GetSingle(varName);
                                data.Variables.Set(new CVar(variableName, single.Split(new string[] { ReplaceValues(splitSeparator, data) }, StringSplitOptions.None).ToList(), isCapture));
                                break;
                        }
                        data.Log(new LogEntry($"Executed action {varAction} on variable {varName}", Colors.White));
                        break;

                    case UtilityGroup.Conversion:
                        byte[] convertedBytes = ReplaceValues(inputString, data).ConvertFrom(conversionFrom);
                        data.Variables.Set(new CVar(variableName, convertedBytes.ConvertTo(conversionTo), isCapture));
                        data.Log(new LogEntry($"Converted input {conversionFrom} to {conversionTo}", Colors.White));
                        break;

                    case UtilityGroup.File:
                        var file = ReplaceValues(filePath, data);
                        var input = ReplaceValues(inputString, data).Replace("\\r\\n", "\r\n").Replace("\\n", "\n");
                        var inputs = ReplaceValuesRecursive(inputString, data).Select(i => i.Replace("\\r\\n", "\r\n").Replace("\\n", "\n"));
                        switch (fileAction)
                        {
                            case FileAction.Read:
                                data.Variables.Set(new CVar(variableName, File.ReadAllText(file), isCapture));
                                break;

                            case FileAction.ReadLines:
                                data.Variables.Set(new CVar(variableName, File.ReadAllLines(file).ToList(), isCapture));
                                break;

                            case FileAction.Write:
                                File.WriteAllText(file, input);
                                break;

                            case FileAction.WriteLines:
                                File.WriteAllLines(file, inputs);
                                break;

                            case FileAction.Append:
                                File.AppendAllText(file, input);
                                break;

                            case FileAction.AppendLines:
                                File.AppendAllLines(file, inputs);
                                break;
                        }
                        data.Log(new LogEntry($"Executed action {fileAction} on file {file}", Colors.White));
                        break;

                    default:
                        break;
                }
            }
            catch(Exception ex) { data.Log(new LogEntry(ex.Message, Colors.Tomato)); }
        }
    }
}
