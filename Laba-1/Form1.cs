using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;


namespace Laba_1
{
    public partial class Compiler : Form
    {
        public Compiler()
        {
            InitializeComponent();

            var toolTip = new ToolTip
            {
                ShowAlways = true,
                InitialDelay = 200,
                ReshowDelay = 100,
                AutoPopDelay = 5000
            };

            // Устанавливаем подсказки для кнопок
            toolTip.SetToolTip(createDocumentButton, "Создать документ");
            toolTip.SetToolTip(openDocumentButton, "Открыть документ");
            toolTip.SetToolTip(saveDocumentButton, "Сохранить документ");
            toolTip.SetToolTip(returnBackButton, "Отменить действие");
            toolTip.SetToolTip(returnForwardButton, "Повторить действие");
            toolTip.SetToolTip(copyTextButton, "Копировать");
            toolTip.SetToolTip(cutOutButton, "Вырезать");
            toolTip.SetToolTip(insertButton, "Вставить");
            toolTip.SetToolTip(startButton, "Пуск анализа");
            toolTip.SetToolTip(faqButton, "Вызов справки");
            toolTip.SetToolTip(informationButton, "О программе");

            this.FormClosing += Compiler_FormClosing;

            SetupDataGridView();
            tabControl1.TabPages.Clear();
            tabControl1.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl1.DrawItem += tabControl1_DrawItem;
            tabControl1.MouseDown += tabControl1_MouseDown;
            this.KeyPreview = true;
            this.KeyDown += Compiler_KeyDown;

            startButton.Click += startButton_Click;

            // Разрешаем перетаскивание файлов в окно
            this.AllowDrop = true;
            this.DragEnter += Compiler_DragEnter;
            this.DragDrop += Compiler_DragDrop;
        }

        private void Compiler_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Пример Prompt-сохранения для всех вкладок:
            foreach (TabPage tab in tabControl1.TabPages)
            {
                if (tab.Controls.Count == 0) continue;
                if (tab.Controls[0] is RichTextBox rtb
                    && rtb.Tag is DocumentInfo info
                    && info.IsModified)
                {
                    DialogResult dr = MessageBox.Show(
                        $"Сохранить изменения в \"{tab.Text.TrimEnd('*')}\"?",
                        "Сохранение", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

                    if (dr == DialogResult.Yes)
                    {
                        tabControl1.SelectedTab = tab;
                        сохранитьToolStripMenuItem_Click(sender, e);
                    }
                    else if (dr == DialogResult.Cancel)
                    {
                        e.Cancel = true;  // отменяем закрытие всей формы
                        return;
                    }
                }
            }
            // если дошли сюда — можно закрывать форму
        }

        private void Compiler_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void Compiler_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || files.Length == 0)
                return;

            foreach (string file in files)
            {
                if (File.Exists(file))
                {
                    // Создаем новую вкладку с именем файла
                    TabPage newTab = new TabPage(Path.GetFileName(file));
                    RichTextBox rtb = new RichTextBox
                    {
                        Dock = DockStyle.Fill,
                        WordWrap = false,
                        ScrollBars = RichTextBoxScrollBars.Both,
                        RightMargin = int.MaxValue
                    };

                    DocumentInfo docInfo = new DocumentInfo
                    {
                        FilePath = file,
                        IsModified = false
                    };
                    rtb.Tag = docInfo;

                    try
                    {
                        rtb.Text = File.ReadAllText(file);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при открытии файла: " + ex.Message);
                        continue;
                    }

                    rtb.TextChanged += (s, ev) =>
                    {
                        docInfo.IsModified = true;
                        if (!newTab.Text.EndsWith("*"))
                            newTab.Text += "*";
                    };

                    newTab.Controls.Add(rtb);
                    tabControl1.TabPages.Add(newTab);
                    tabControl1.SelectedTab = newTab;
                }
            }
        }

        private void Compiler_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control)
            {
                // Получаем активный RichTextBox из выбранной вкладки
                RichTextBox rtb = GetActiveRichTextBox();
                if (rtb == null) return;

                // Обработка Ctrl + +
                if (e.KeyCode == Keys.Oemplus)
                {
                    // Увеличиваем масштаб
                    ChangeZoom(rtb, +0.1f);
                    e.Handled = true;
                }
                // Обработка Ctrl + -
                else if (e.KeyCode == Keys.OemMinus)
                {
                    // Уменьшаем масштаб
                    ChangeZoom(rtb, -0.1f);
                    e.Handled = true;
                }
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            // Если зажата Ctrl, то меняем масштаб, иначе — обычная прокрутка
            if ((ModifierKeys & Keys.Control) == Keys.Control)
            {
                RichTextBox rtb = GetActiveRichTextBox();
                if (rtb != null)
                {
                    // e.Delta > 0 — прокрутка вверх (увеличить)
                    // e.Delta < 0 — прокрутка вниз (уменьшить)
                    float delta = (e.Delta > 0) ? +0.1f : -0.1f;
                    ChangeZoom(rtb, delta);
                }
            }
            else
            {
                // Базовое поведение, чтобы обычная прокрутка работала
                base.OnMouseWheel(e);
            }
        }

        private RichTextBox GetActiveRichTextBox()
        {
            if (tabControl1.TabPages.Count == 0)
                return null;
            TabPage activeTab = tabControl1.SelectedTab;
            if (activeTab == null || activeTab.Controls.Count == 0)
                return null;

            return activeTab.Controls[0] as RichTextBox;
        }

        private void ChangeZoom(RichTextBox rtb, float delta)
        {
            float newZoom = rtb.ZoomFactor + delta;
            // Ограничим масштаб, например, от 0.5 (50%) до 5.0 (500%)
            if (newZoom < 0.5f) newZoom = 0.5f;
            if (newZoom > 5.0f) newZoom = 5.0f;

            rtb.ZoomFactor = newZoom;
        }

        // Метод для создания новой вкладки с RichTextBox
        private void CreateNewTab(string tabTitle)
        {
            TabPage newTab = new TabPage(tabTitle);
            RichTextBox rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both
            };

            // Создаём объект DocumentInfo для хранения пути к файлу и статуса изменений
            DocumentInfo docInfo = new DocumentInfo();
            rtb.Tag = docInfo;

            // При изменении текста помечаем документ как изменённый и добавляем звездочку в заголовок вкладки
            rtb.TextChanged += (s, e) =>
            {
                docInfo.IsModified = true;
                if (!newTab.Text.EndsWith("*"))
                    newTab.Text += "*";
            };

            newTab.Controls.Add(rtb);
            tabControl1.TabPages.Add(newTab);
            tabControl1.SelectedTab = newTab;
        }

        // Метод отрисовки вкладки с крестиком
        private void tabControl1_DrawItem(object sender, DrawItemEventArgs e)
        {
            try
            {
                TabPage tabPage = tabControl1.TabPages[e.Index];
                Rectangle tabRect = tabControl1.GetTabRect(e.Index);

                // Проверка, является ли вкладка активной
                bool isActiveTab = e.Index == tabControl1.SelectedIndex;

                // Отображаем фоновый цвет вкладки (выделенная вкладка будет другой)
                if (isActiveTab)
                {
                    e.Graphics.FillRectangle(Brushes.LightBlue, tabRect); // Цвет для активной вкладки
                }
                else
                {
                    e.Graphics.FillRectangle(Brushes.White, tabRect); // Цвет для неактивных вкладок
                }

                // Центрируем текст в пределах вкладки
                var textSize = TextRenderer.MeasureText(tabPage.Text, tabControl1.Font);
                var textPosition = new Point(tabRect.X + (tabRect.Width - textSize.Width) / 2, tabRect.Y + (tabRect.Height - textSize.Height) / 2);

                // Отрисовываем текст вкладки
                TextRenderer.DrawText(e.Graphics, tabPage.Text, tabControl1.Font, textPosition, SystemColors.ControlText);
                
                tabControl1.SizeMode = TabSizeMode.Normal;

                // Определяем размеры крестика
                int closeButtonSize = 15;
                Rectangle closeButtonRect = new Rectangle(
                    tabRect.Right - closeButtonSize - 5,  // 5px отступ от правого края вкладки
                    tabRect.Top + (tabRect.Height - closeButtonSize) / 2,  // Центрируем крестик по высоте вкладки
                    closeButtonSize, closeButtonSize);

                // Отображаем символ "✕"
                using (Font font = new Font("Arial", 12, FontStyle.Bold))
                {
                    e.Graphics.DrawString("✕", font, Brushes.Black, closeButtonRect);
                }

                // Добавляем эффект для активной вкладки (например, слегка тень под текстом или подсветка)
                if (isActiveTab)
                {
                    e.Graphics.DrawRectangle(Pens.Black, tabRect);
                }
            }
            catch (Exception ex)
            {
                // Обработка ошибок отрисовки
            }
        }

        // Обработчик клика мыши для определения нажатия на крестик
        private void tabControl1_MouseDown(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < tabControl1.TabPages.Count; i++)
            {
                Rectangle tabRect = tabControl1.GetTabRect(i);
                int closeButtonSize = 15;
                Rectangle closeButtonRect = new Rectangle(
                    tabRect.Right - closeButtonSize - 5,
                    tabRect.Top + (tabRect.Height - closeButtonSize) / 2,
                    closeButtonSize, closeButtonSize);

                if (closeButtonRect.Contains(e.Location))
                {
                    TabPage tab = tabControl1.TabPages[i];
                    // Если документ изменён, спрашиваем о сохранении
                    if (tab.Controls.Count > 0 && tab.Controls[0] is RichTextBox rtb)
                    {
                        DocumentInfo docInfo = rtb.Tag as DocumentInfo;
                        if (docInfo != null && docInfo.IsModified)
                        {
                            DialogResult dr = MessageBox.Show(
                                $"Сохранить изменения в \"{tab.Text.TrimEnd('*')}\"?",
                                "Сохранение", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                            if (dr == DialogResult.Yes)
                            {
                                // Вызываем ваш метод сохранения (например, тот же, что используется в меню)
                                сохранитьToolStripMenuItem_Click(sender, e);
                            }
                            else if (dr == DialogResult.Cancel)
                            {
                                return; // Отмена закрытия вкладки
                            }
                        }
                    }
                    // Закрываем вкладку
                    tabControl1.TabPages.Remove(tab);
                    break;
                }
            }
        }

        // Обработчик пункта меню "Создать"
        private void создатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateNewTab("Новый документ");
        }

        // Обработчик пункта меню "Открыть"
        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    TabPage newTab = new TabPage(Path.GetFileName(ofd.FileName));
                    RichTextBox rtb = new RichTextBox { Dock = DockStyle.Fill };

                    DocumentInfo docInfo = new DocumentInfo
                    {
                        FilePath = ofd.FileName,
                        IsModified = false
                    };
                    rtb.Tag = docInfo;

                    try
                    {
                        rtb.Text = File.ReadAllText(ofd.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при открытии файла: " + ex.Message);
                        return;
                    }

                    rtb.TextChanged += (s, ev) =>
                    {
                        docInfo.IsModified = true;
                        if (!newTab.Text.EndsWith("*"))
                            newTab.Text += "*";
                    };

                    newTab.Controls.Add(rtb);
                    tabControl1.TabPages.Add(newTab);
                    tabControl1.SelectedTab = newTab;
                }
            }
        }

        // Обработчик пункта меню "Сохранить"
        private void сохранитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.TabPages.Count == 0)
                return;

            TabPage activeTab = tabControl1.SelectedTab;
            if (activeTab == null || activeTab.Controls.Count == 0)
                return;

            RichTextBox rtb = activeTab.Controls[0] as RichTextBox;
            DocumentInfo docInfo = rtb.Tag as DocumentInfo;

            // Если путь не задан, вызываем "Сохранить как"
            if (string.IsNullOrEmpty(docInfo.FilePath))
            {
                сохранитьКакToolStripMenuItem_Click(sender, e);
            }
            else
            {
                SaveDocument(rtb, docInfo, activeTab);
            }
        }

        // Метод сохранения документа по указанному пути
        private void SaveDocument(RichTextBox rtb, DocumentInfo docInfo, TabPage tab)
        {
            try
            {
                File.WriteAllText(docInfo.FilePath, rtb.Text);
                docInfo.IsModified = false;
                if (tab.Text.EndsWith("*"))
                    tab.Text = tab.Text.TrimEnd('*');
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении файла: " + ex.Message);
            }
        }

        // Обработчик пункта меню "Сохранить как"
        private void сохранитьКакToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.TabPages.Count == 0)
                return;

            TabPage activeTab = tabControl1.SelectedTab;
            if (activeTab == null || activeTab.Controls.Count == 0)
                return;

            RichTextBox rtb = activeTab.Controls[0] as RichTextBox;
            DocumentInfo docInfo = rtb.Tag as DocumentInfo;

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    docInfo.FilePath = sfd.FileName;
                    SaveDocument(rtb, docInfo, activeTab);
                    activeTab.Text = Path.GetFileName(sfd.FileName);
                }
            }
        }

        // Обработчик пункта меню "Выход"
        private void выходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Проходим по всем вкладкам и, если найдены несохранённые изменения, предлагаем сохранить их
            foreach (TabPage tab in tabControl1.TabPages)
            {
                if (tab.Controls.Count == 0)
                    continue;
                RichTextBox rtb = tab.Controls[0] as RichTextBox;
                DocumentInfo docInfo = rtb.Tag as DocumentInfo;
                if (docInfo.IsModified)
                {
                    DialogResult dr = MessageBox.Show(
                        $"Сохранить изменения в \"{tab.Text.TrimEnd('*')}\"?",
                        "Сохранение",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Warning);
                    if (dr == DialogResult.Yes)
                    {
                        tabControl1.SelectedTab = tab;
                        сохранитьToolStripMenuItem_Click(sender, e);
                    }
                    else if (dr == DialogResult.Cancel)
                    {
                        return;
                    }
                }
            }
            Application.Exit();
        }

        // Обработчик для пункта "Отменить"
        private void отменитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.TabPages.Count == 0)
                return;

            TabPage activeTab = tabControl1.SelectedTab;
            RichTextBox rtb = activeTab.Controls[0] as RichTextBox;
            if (rtb != null && rtb.CanUndo)
            {
                rtb.Undo();
            }
        }

        // Обработчик для пункта "Повторить"
        private void повторитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.TabPages.Count == 0)
                return;

            TabPage activeTab = tabControl1.SelectedTab;
            RichTextBox rtb = activeTab.Controls[0] as RichTextBox;
            if (rtb != null && rtb.CanRedo)
            {
                rtb.Redo();
            }
        }

        // Обработчик для пункта "Вырезать"
        private void вырезатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.TabPages.Count == 0)
                return;

            TabPage activeTab = tabControl1.SelectedTab;
            RichTextBox rtb = activeTab.Controls[0] as RichTextBox;
            if (rtb != null)
            {
                rtb.Cut();
            }
        }

        // Обработчик для пункта "Копировать"
        private void копироватьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.TabPages.Count == 0)
                return;

            TabPage activeTab = tabControl1.SelectedTab;
            RichTextBox rtb = activeTab.Controls[0] as RichTextBox;
            if (rtb != null)
            {
                rtb.Copy();
            }
        }

        // Обработчик для пункта "Вставить"
        private void вставитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.TabPages.Count == 0)
                return;

            TabPage activeTab = tabControl1.SelectedTab;
            RichTextBox rtb = activeTab.Controls[0] as RichTextBox;
            if (rtb != null)
            {
                rtb.Paste();
            }
        }

        // Обработчик для пункта "Удалить"
        // Здесь мы просто удаляем выделенный текст (аналог действия "Delete")
        private void удалитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.TabPages.Count == 0)
                return;

            TabPage activeTab = tabControl1.SelectedTab;
            RichTextBox rtb = activeTab.Controls[0] as RichTextBox;
            if (rtb != null)
            {
                rtb.SelectedText = "";
            }
        }

        // Обработчик для пункта "Выделить все"
        private void выделитьВсеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tabControl1.TabPages.Count == 0)
                return;

            TabPage activeTab = tabControl1.SelectedTab;
            RichTextBox rtb = activeTab.Controls[0] as RichTextBox;
            if (rtb != null)
            {
                rtb.SelectAll();
            }
        }

        private void вызовСправкиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // HTML-контент справки
            string html = @"<!DOCTYPE html>
<html lang=""ru"">
<head>
  <meta charset=""utf-8"">
  <title>Справка</title>
  <style>
    body { font-family: Arial, sans-serif; margin: 20px; line-height: 1.5; }
    h1 { margin-bottom: 0.5em; }
    h2 { margin-top: 1em; margin-bottom: 0.3em; }
    ul { margin-top: 0; }
  </style>
</head>
<body>
  <h1>Справка</h1>
  <h2>Меню «Файл»:</h2>
  <ul>
    <li><b>Создать</b> – создаёт новый документ во вкладке.</li>
    <li><b>Открыть</b> – открывает существующий файл из указанного пути.</li>
    <li><b>Сохранить</b> – сохраняет текущий документ, если путь к файлу уже задан; в противном случае вызывает диалог «Сохранить как».</li>
    <li><b>Сохранить как</b> – позволяет выбрать путь и имя для сохранения текущего документа.</li>
    <li><b>Выход</b> – закрывает приложение; при наличии несохранённых изменений предлагает сохранить их.</li>
  </ul>

  <h2>Меню «Правка»:</h2>
  <ul>
    <li><b>Отменить</b> – отменяет последнее действие (если возможно).</li>
    <li><b>Повторить</b> – повторяет отменённое действие (если возможно).</li>
    <li><b>Вырезать</b> – вырезает выделенный фрагмент текста в буфер обмена.</li>
    <li><b>Копировать</b> – копирует выделенный фрагмент текста в буфер обмена.</li>
    <li><b>Вставить</b> – вставляет содержимое буфера обмена в текущую позицию курсора.</li>
    <li><b>Удалить</b> – удаляет выделенный фрагмент текста без помещения в буфер обмена.</li>
    <li><b>Выделить все</b> – выделяет весь текст в активном документе.</li>
  </ul>

  <h2>Меню «Справка»:</h2>
  <ul>
    <li><b>Вызов справки</b> – открывает данный HTML-документ со справочной информацией.</li>
    <li><b>О программе</b> – выводит сведения о версии приложения, авторах, дате создания и т.п.</li>
  </ul>
</body>
</html>";

            // Записываем во временный файл
            string tempPath = Path.Combine(Path.GetTempPath(), "CompilerHelp.html");
            File.WriteAllText(tempPath, html, Encoding.UTF8);

            // Открываем в браузере по умолчанию
            Process.Start(new ProcessStartInfo
            {
                FileName = tempPath,
                UseShellExecute = true
            });
        }


        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void outputTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void SetupDataGridView()
        {
            dataGridViewTokens.Columns.Clear();
            dataGridViewTokens.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dataGridViewTokens.Columns.Add("Code", "Код");
            dataGridViewTokens.Columns.Add("Type", "Тип лексемы");
            dataGridViewTokens.Columns.Add("Lexeme", "Лексема");
            dataGridViewTokens.Columns.Add("Position", "Положение");
        }


        /// <summary>
        /// Возвращает абсолютный номер символа в тексте (начиная с 1),
        /// по номеру строки и позиции в строке.
        /// </summary>
        private int CalculateAbsolutePosition(int line, int column)
        {
            // разбиваем документ на строки по '\n'
            var lines = GetActiveRichTextBox().Text.Split('\n');
            int pos = 0;
            // суммируем длины всех предыдущих строк + по одному символу переноса строки
            for (int i = 0; i < line - 1 && i < lines.Length; i++)
                pos += lines[i].Length + 1;
            // добавляем смещение внутри строки
            pos += column;
            return pos;
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            splitContainer2.Panel2.Controls.Clear();

            var rtb = GetActiveRichTextBox();
            if (rtb == null)
            {
                MessageBox.Show("Нет активного документа для анализа!");
                return;
            }
            string text = rtb.Text;

            // Наше регулярное выражение для 7-значного номера xxx-xx-xx
            //var pattern = @"\b\d+(?:,\d+)?\b";
            //var matches = Regex.Matches(text, pattern);

            //if (matches.Count == 0)
            //{
            //    splitContainer2.Panel2.Controls.Add(new RichTextBox
            //    {
            //        Dock = DockStyle.Fill,
            //        ReadOnly = true,
            //        Text = "Номера не найдены."
            //    });
            //    return;
            //}

            //// Таблица для вывода найденных номеров
            //var dgv = new DataGridView
            //{
            //    Dock = DockStyle.Fill,
            //    ReadOnly = true,
            //    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            //};
            //dgv.Columns.Add("Number", "Найденный номер");
            //dgv.Columns.Add("Position", "Позиция");

            //foreach (Match m in matches)
            //{
            //    int absPos = CalculateAbsolutePositionByIndex(text, m.Index);
            //    dgv.Rows.Add(m.Value, absPos);
            //}

            //splitContainer2.Panel2.Controls.Add(dgv);

            // 1) Создаём автомат
            var automaton = new NumberAutomaton();

            // 2) Ищем все вхождения
            var found = automaton.FindMatches(text);

            if (found.Count == 0)
            {
                splitContainer2.Panel2.Controls.Add(new RichTextBox
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    Text = "Числа не найдены."
                });
                return;
            }

            // 3) Выводим в DataGridView
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgv.Columns.Add("Value", "Найденное число");
            dgv.Columns.Add("Pos", "Позиция (индекс 1-based)");

            foreach (var (start, length, value) in found)
            {
                dgv.Rows.Add(value, start + 1);
            }

            splitContainer2.Panel2.Controls.Add(dgv);
        }

        private int CalculateAbsolutePositionByIndex(string text, int index)
        {
            // если вам нужна просто index+1, можно вернуть index+1
            return index + 1;
        }

        private void пускToolStripMenuItem_Click(object sender, EventArgs e)
        {
            startButton_Click(sender, e);
        }

        private void informationButton_Click(object sender, EventArgs e)
        {
            оПрограммеToolStripMenuItem_Click(sender, e);
        }

        private void оПрограммеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Создаем экземпляр окна "О программе"
            Form aboutForm = new Form()
            {
                Text = "О программе",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                Size = new Size(400, 250)
            };

            // Создаем элемент управления для вывода информации
            Label lblInfo = new Label()
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 10),
                // Здесь можно указать любую информацию о программе
                Text = "Программа \"Compiler\"\nВерсия 0.2.2\nАвтор: Фролов Марк Евгеньевич\n\nОписание: Сканер для анализа кода.\n2025 г."
            };

            // Добавляем метку в окно
            aboutForm.Controls.Add(lblInfo);

            // Отображаем окно модально
            aboutForm.ShowDialog();
        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void постановкаЗадачиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // создаём модальное окно
            var taskForm = new Form
            {
                Text = "Постановка задачи",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                Size = new Size(600, 400)
            };

            // текст с переносами строк
            string text =
                "";

            // используем RichTextBox для удобного отображения многострочного текста
            var rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10),
                Text = text
            };

            taskForm.Controls.Add(rtb);
            taskForm.ShowDialog();
        }

        private void грамматикаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // создаём модальное окно
            var frm = new Form
            {
                Text = "Грамматика",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                Size = new Size(700, 600)
            };

            // многострочный текст грамматики
            string text = @"";

            // используем RichTextBox для удобства прокрутки и переноса строк
            var rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 10),
                Text = text
            };

            frm.Controls.Add(rtb);
            frm.ShowDialog();
        }

        private void классификацияГрамматикиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var frm = new Form
            {
                Text = "Классификация грамматики",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                Size = new Size(600, 350)
            };

            string text =
        @"но";

            var rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10),
                Text = text
            };

            frm.Controls.Add(rtb);
            frm.ShowDialog();
        }

        private void методАнализаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Пути к изображениям
            string appDir = Application.StartupPath;
            string img1 = Path.Combine(appDir, "MethodAnalysis1.png");
            string img2 = Path.Combine(appDir, "MethodAnalysis2.png");

            if (!File.Exists(img1) || !File.Exists(img2))
            {
                MessageBox.Show(
                    "Невозможно найти один или оба файла:\n" +
                    img1 + "\n" + img2,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

        // Содержимое HTML
        string html = $@"
";

            // Путь к временному HTML-файлу
            string htmlPath = Path.Combine(Path.GetTempPath(), "MethodAnalysis.html");
            File.WriteAllText(htmlPath, html, Encoding.UTF8);

            // Открываем в браузере
            Process.Start(new ProcessStartInfo
            {
                FileName = htmlPath,
                UseShellExecute = true
            });
        }

        private void тестовыйПримерToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Название вкладки
            var newTab = new TabPage("Тестовый пример");

            // Сам редактор
            var rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both,
                Font = new Font("Consolas", 10),
                Text = "val animals = listOf(\"Dog\", \"Cat\", \"Cow\");"
            };

            // Разметка «есть несохранённые изменения»
            var info = new DocumentInfo { FilePath = string.Empty, IsModified = true };
            rtb.Tag = info;
            rtb.TextChanged += (s, ev) =>
            {
                info.IsModified = true;
                if (!newTab.Text.EndsWith("*"))
                    newTab.Text += "*";
            };

            // Добавляем всё в TabControl
            newTab.Controls.Add(rtb);
            tabControl1.TabPages.Add(newTab);
            tabControl1.SelectedTab = newTab;
        }

        private void списокЛитературыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var frm = new Form
            {
                Text = "Список литературы",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                Size = new Size(700, 350)
            };

            string text =
        @"";

            var rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10),
                Text = text
            };

            frm.Controls.Add(rtb);
            frm.ShowDialog();
        }

        private void диагностикаИНейтрализацияОшибокToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var frm = new Form
            {
                Text = "Диагностика и нейтрализация ошибок",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                Size = new Size(600, 300)
            };

            string text = @"";

            var rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10),
                Text = text
            };

            frm.Controls.Add(rtb);
            frm.ShowDialog();
        }

        private void исходныйКодПрограммыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var frm = new Form
            {
                Text = "Листинг программы",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.SizableToolWindow,
                Width = 800,
                Height = 600
            };

            string listing = @"";

            var rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both,
                Font = new Font("Consolas", 9),
                Text = listing
            };

            frm.Controls.Add(rtb);
            frm.ShowDialog();
        }

    }

    // Вспомогательный класс для хранения информации о документе
    public class DocumentInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public bool IsModified { get; set; } = false;
    }
}