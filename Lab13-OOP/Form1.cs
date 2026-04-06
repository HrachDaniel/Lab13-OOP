using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lab13_OOP
{
    public partial class Form1 : Form
    {
        // Змінна для зберігання поточного шляху
        private string currentPath = "";

        public Form1()
        {
            InitializeComponent();
        }

        // =========================================================
        // 1. ЗАВАНТАЖЕННЯ СПИСКУ ДИСКІВ
        // =========================================================
        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                DriveInfo[] drives = DriveInfo.GetDrives();
                foreach (DriveInfo drive in drives)
                {
                    if (drive.IsReady) // Додаємо тільки готові диски (без порожніх дисководів)
                    {
                        cmbDrives.Items.Add(drive.Name);
                    }
                }

                if (cmbDrives.Items.Count > 0)
                    cmbDrives.SelectedIndex = 0; // Викличе подію SelectedIndexChanged
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка завантаження дисків: " + ex.Message);
            }
        }

        // =========================================================
        // 2. ЗМІНА ДИСКА ТА ВІДОБРАЖЕННЯ ЙОГО ВЛАСТИВОСТЕЙ
        // =========================================================
        private void cmbDrives_SelectedIndexChanged(object sender, EventArgs e)
        {
            string driveName = cmbDrives.SelectedItem.ToString();
            LoadDirectory(driveName);

            // Виведення властивостей диска
            try
            {
                DriveInfo di = new DriveInfo(driveName);
                txtProperties.Text = $"=== ВЛАСТИВОСТІ ДИСКА ===\r\n" +
                                     $"Назва: {di.Name}\r\n" +
                                     $"Мітка тому: {di.VolumeLabel}\r\n" +
                                     $"Файлова система: {di.DriveFormat}\r\n" +
                                     $"Тип: {di.DriveType}\r\n" +
                                     $"Загальний розмір: {di.TotalSize / 1024 / 1024} МБ\r\n" +
                                     $"Вільного місця: {di.AvailableFreeSpace / 1024 / 1024} МБ\r\n";
                tabControl1.SelectedIndex = 0;
            }
            catch { }
        }

        // =========================================================
        // 3. ПЕРЕМІЩЕННЯ ПО ФАЙЛОВІЙ СИСТЕМІ ТА ФІЛЬТРАЦІЯ
        // =========================================================
        private void LoadDirectory(string path)
        {
            try
            {
                currentPath = path;
                lstFolders.Items.Clear();
                lstFiles.Items.Clear();

                // Отримуємо фільтри (якщо порожньо - ставимо зірочку для пошуку всього)
                string folderFilter = string.IsNullOrWhiteSpace(txtFolderFilter.Text) ? "*" : txtFolderFilter.Text;
                string fileFilter = string.IsNullOrWhiteSpace(txtFileFilter.Text) ? "*.*" : txtFileFilter.Text;

                // Завантажуємо каталоги
                string[] dirs = Directory.GetDirectories(path, folderFilter);
                foreach (string d in dirs)
                {
                    lstFolders.Items.Add(Path.GetFileName(d));
                }

                // Завантажуємо файли
                string[] files = Directory.GetFiles(path, fileFilter);
                foreach (string f in files)
                {
                    lstFiles.Items.Add(Path.GetFileName(f));
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Доступ до цієї папки заборонено (потрібні права адміністратора)!", "Відмова в доступі");
                // Повертаємось на рівень вище
                NavigateUp();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка: " + ex.Message);
            }
        }

        // Кнопка "Вгору"
        private void btnUp_Click(object sender, EventArgs e)
        {
            NavigateUp();
        }

        private void NavigateUp()
        {
            if (!string.IsNullOrEmpty(currentPath))
            {
                DirectoryInfo parent = Directory.GetParent(currentPath);
                if (parent != null)
                {
                    LoadDirectory(parent.FullName);
                }
            }
        }

        // Подвійний клік по папці - вхід у неї
        private void lstFolders_DoubleClick(object sender, EventArgs e)
        {
            if (lstFolders.SelectedItem != null)
            {
                string newPath = Path.Combine(currentPath, lstFolders.SelectedItem.ToString());
                LoadDirectory(newPath);
            }
        }

        // =========================================================
        // 4. ВЛАСТИВОСТІ ТА БЕЗПЕКА КАТАЛОГУ
        // =========================================================
        private void lstFolders_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstFolders.SelectedItem == null) return;
            string fullPath = Path.Combine(currentPath, lstFolders.SelectedItem.ToString());

            try
            {
                DirectoryInfo di = new DirectoryInfo(fullPath);
                txtProperties.Text = $"=== ВЛАСТИВОСТІ ПАПКИ ===\r\n" +
                                     $"Назва: {di.Name}\r\n" +
                                     $"Створено: {di.CreationTime}\r\n" +
                                     $"Атрибути: {di.Attributes}\r\n\r\n" +
                                     $"=== АТРИБУТИ БЕЗПЕКИ (ACL) ===\r\n";

                // Читання прав доступу (Security Attributes)
                DirectorySecurity ds = di.GetAccessControl();
                foreach (FileSystemAccessRule rule in ds.GetAccessRules(true, true, typeof(NTAccount)))
                {
                    txtProperties.Text += $"- {rule.IdentityReference}: {rule.FileSystemRights} ({rule.AccessControlType})\r\n";
                }
                tabControl1.SelectedIndex = 0;
            }
            catch { txtProperties.Text += "\r\n[Немає доступу для читання атрибутів безпеки]"; }
        }

        // =========================================================
        // 5. ВЛАСТИВОСТІ ФАЙЛУ ТА ПЕРЕГЛЯД ВМІСТУ (Текст / Зображення)
        // =========================================================
        private void lstFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstFiles.SelectedItem == null) return;
            string fullPath = Path.Combine(currentPath, lstFiles.SelectedItem.ToString());

            try
            {
                FileInfo fi = new FileInfo(fullPath);
                txtProperties.Text = $"=== ВЛАСТИВОСТІ ФАЙЛУ ===\r\n" +
                                     $"Назва: {fi.Name}\r\n" +
                                     $"Розмір: {fi.Length} байт\r\n" +
                                     $"Створено: {fi.CreationTime}\r\n" +
                                     $"Змінено: {fi.LastWriteTime}\r\n" +
                                     $"Атрибути: {fi.Attributes}\r\n\r\n" +
                                     $"=== АТРИБУТИ БЕЗПЕКИ (ACL) ===\r\n";

                // Читання прав доступу
                FileSecurity fs = fi.GetAccessControl();
                foreach (FileSystemAccessRule rule in fs.GetAccessRules(true, true, typeof(NTAccount)))
                {
                    txtProperties.Text += $"- {rule.IdentityReference}: {rule.FileSystemRights} ({rule.AccessControlType})\r\n";
                }

                // Виклик методу попереднього перегляду
                PreviewFile(fullPath);
            }
            catch { }
        }

        private void PreviewFile(string path)
        {
            // Очищення попереднього перегляду
            if (picPreview.Image != null)
            {
                picPreview.Image.Dispose();
                picPreview.Image = null;
            }
            txtPreview.Text = "";

            string ext = Path.GetExtension(path).ToLower();

            // Перегляд зображень
            if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".gif")
            {
                try
                {
                    picPreview.Image = Image.FromFile(path);
                    tabControl1.SelectedIndex = 2; // Перемикаємо на вкладку "Зображення"
                }
                catch { }
            }
            // Перегляд текстових файлів
            else if (ext == ".txt" || ext == ".log" || ext == ".cs" || ext == ".xml" || ext == ".json")
            {
                try
                {
                    FileInfo fi = new FileInfo(path);
                    if (fi.Length < 1048576) // Читаємо тільки файли до 1 МБ, щоб не зависла програма
                    {
                        txtPreview.Text = File.ReadAllText(path);
                        tabControl1.SelectedIndex = 1; // Перемикаємо на вкладку "Текст"
                    }
                    else
                    {
                        txtPreview.Text = "Файл занадто великий для попереднього перегляду (>1 МБ).";
                        tabControl1.SelectedIndex = 1;
                    }
                }
                catch { }
            }
            else
            {
                tabControl1.SelectedIndex = 0; // Для інших файлів показуємо тільки властивості
            }
        }

        // =========================================================
        // 6. ЗАСТОСУВАННЯ ФІЛЬТРІВ ПРИ НАТИСКАННІ ENTER АБО ЗМІНІ
        // =========================================================
        private void txtFolderFilter_TextChanged(object sender, EventArgs e)
        {
            if (Directory.Exists(currentPath)) LoadDirectory(currentPath);
        }

        private void txtFileFilter_TextChanged(object sender, EventArgs e)
        {
            if (Directory.Exists(currentPath)) LoadDirectory(currentPath);
        }
    }
}