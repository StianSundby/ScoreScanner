using IronXL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ScoreScanner.Services
{
    internal class FileProcessor
    {
        public static List<string>? _files = new();
        public static string FindNextCoXKill(string path, int n)
        {
            path = path.Replace(@"\\", @"\");
            string resultImage = string.Empty;

            if (_files == null || !_files.Any() || _files.Count == 0) UpdateFiles(path);
            //foreach (var file in _files!.Where(file => file.Contains(n.ToString()))) resultImage = file;
            foreach (var image in _files!)
            {
                if (image.Contains(n.ToString()))
                {
                    resultImage = image;
                    break;
                }
            }
            
            return resultImage!;
        }


        public static string FindLastCoXKill(string path)
        {
            path = path.Replace(@"\\", @"\");
            string resultImage = string.Empty;

            if (_files == null || !_files.Any()) UpdateFiles(path);
            var convertedFileList = (from number in _files!.Select(file => Regex.Match(file, @"\([0-9]+\)").Value)
                               select number.Replace("(", "") into cleanNumber
                               select cleanNumber.Replace(")", "")into cleanNumber
                               select Convert.ToInt32(cleanNumber)).ToList();
            convertedFileList.Sort();
            var kc = convertedFileList[^1];

            foreach (var file in _files!.Where(file => file.Contains("Xeric(" + kc + ")"))) 
                resultImage = file;
            return resultImage;
        }


        public static void UpdateFiles(string path)
        {
            var tempFileList = new List<string>(Directory.GetFiles(path));
            _files = tempFileList.Where(t => t.Contains("Chambers") && !t.Contains("Challenge")).ToList();
            _files.Sort();
        }

        public static string FindNextEmptyCell(string filePath, int column)
        {
            var wb = WorkBook.Load(filePath);
            var sheet = wb.WorkSheets.First();
            var i = 0;
            var nextCell = string.Empty;
            foreach (var cell in sheet.GetColumn(column))
            {
                i++;
                if (!cell.IsEmpty) continue;
                nextCell = cell.AddressString;
                break;
            }
            wb.Close();
            return nextCell;
        }
        

        public static void ChangeCellValue(string filePath, string cell, int value)
        {
            WorkBook wb = WorkBook.Load(filePath);
            WorkSheet ws = wb.WorkSheets.First();
            if (cell.Contains('B'))
            {
                ws[cell].Value = value;
                wb.SaveAs(filePath);
                wb.Close();
                return;
            } 
            else if (cell.Contains('E'))
            {
                var valueEnum = (PartySize)value - 1;
                ws[cell].Value = valueEnum.ToString();
                wb.SaveAs(filePath);
                wb.Close();
                return;
            }
        }

        public enum PartySize
        {
            Solo,
            Duo,
            Trio,
            Quad,
            Five,
            Mass
        }
    }
}
