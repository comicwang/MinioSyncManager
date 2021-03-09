using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MinioSyncManager
{
    public static class TextBoxExtention
    {
        public static void BindComplete(this TextBox textBox, EventTrigger eventTrigger)
        {
            TextBoxBind textBoxBind = new TextBoxBind();
            textBoxBind.BindComplete(textBox, eventTrigger);
        }

        private static string _logFileForlder = Path.Combine(Application.StartupPath, "Logs");

        public static void AppendLog2File(this TextBox textBox)
        {
            if (Directory.Exists(_logFileForlder) == false)
            {
                Directory.CreateDirectory(_logFileForlder);
            }

            DateTime now = DateTime.Now;
            string dateForler = Path.Combine(_logFileForlder, now.ToString("yyyy-MM-dd"));
            if (Directory.Exists(dateForler) == false)
            {
                Directory.CreateDirectory(dateForler);
            }
            if (!string.IsNullOrWhiteSpace(textBox.Text))
            {
                File.WriteAllLines(Path.Combine(dateForler, now.ToString("yyyyMMddHHmmss") + ".log"), textBox.Lines);
                textBox.Clear();
            }
        }
    }

    public class TextBoxBind 
    {
        private string suggestionPath = string.Empty;

        private string[] txtSuggestion
        {
            get
            {
                if (File.Exists(suggestionPath))
                {
                    string[] txts = File.ReadAllLines(suggestionPath);
                    return txts;
                }
                return new string[0];
            }
            set
            {
                if (File.Exists(suggestionPath))
                    File.Delete(suggestionPath);
                File.WriteAllLines(suggestionPath, value);
            }
        }
        public void BindComplete(TextBox textBox, EventTrigger eventTrigger)
        {
            string dir = Path.Combine(Application.StartupPath, "Data");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            suggestionPath = Path.Combine(dir, $"{textBox.Name}.dat");
            if (eventTrigger == EventTrigger.LostFocus)
                textBox.LostFocus += TextBox_LostFocus;
            else if (eventTrigger == EventTrigger.Changed)
                textBox.TextChanged += TextBox_LostFocus;
            textBox.AutoCompleteMode = AutoCompleteMode.Suggest;
            textBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
            AutoCompleteStringCollection autoCompleteStringCollection = new AutoCompleteStringCollection();
            autoCompleteStringCollection.AddRange(txtSuggestion);
            textBox.AutoCompleteCustomSource = autoCompleteStringCollection;
            if (txtSuggestion.Length > 0)
                textBox.Text = txtSuggestion[0];
        }

        private  void TextBox_LostFocus(object sender, EventArgs e)
        {
            TextBox textbox = sender as TextBox;
            string text = textbox.Text;
            if (!txtSuggestion.Contains(text))
            {
                List<string> temp = new List<string>();
                temp.Add(text);
                temp.AddRange(txtSuggestion);
                txtSuggestion = temp.ToArray();
            }
        }
    }

    public enum EventTrigger
    {
        LostFocus,
        Changed
    }
}
