using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using System.ComponentModel;

namespace Item_Viewer_and_Editor
{
    // The main class for the Windows Form application
    public partial class Form1 : Form
    {
        // A class to represent an item from the XML file
        private class Item
        {
            // FIX: Use null-forgiving operator to satisfy non-nullable property requirements
            public string Id { get; set; } = null!;
            public string Name { get; set; } = null!;
            public string Description { get; set; } = null!;
            public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();

            // FIX: Declare Image as nullable since it can be explicitly set to null
            public Bitmap? Image { get; set; }
        }

        // Define an enum for the different themes
        private enum Theme { Light, Dark }

        private List<Item> allItems = new List<Item>();

        // FIX: Use null-forgiving operator for fields initialized outside the constructor
        private string currentFilePath = null!;

        // Constants for dynamic font sizing
        private const float MAX_FONT_SIZE = 10f; // Base font size
        private const float MIN_FONT_SIZE = MAX_FONT_SIZE - 5f; // Minimum font size (5pt)

        // Constant to prevent the description box from growing infinitely tall
        private const int MAX_TEXTBOX_HEIGHT = 400;

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        // FIX: Set 'sender' to nullable (object?) to match the target delegate signature
        private void Form1_Load(object? sender, EventArgs e)
        {
            // Set up UI event handlers
            searchButton.Click += searchButton_Click;
            clearSearchButton.Click += clearSearchButton_Click;
            itemComboBox.SelectedIndexChanged += itemComboBox_SelectedIndexChanged;
            browseButton.Click += browseButton_Click;
            exportSelectedItemButton.Click += exportSelectedItemButton_Click;
            exportAllButton.Click += exportAllButton_Click;
            itemComboBox.DrawItem += itemComboBox_DrawItem;
            themeToggleCheckBox.CheckedChanged += themeToggleCheckBox_CheckedChanged;

            // Set the initial theme to Dark
            SetTheme(Theme.Dark);
            themeToggleCheckBox.Checked = true;

            // Initially, disable search and combo box until a file is loaded.
            searchTextBox.Enabled = false;
            searchButton.Enabled = false;
            clearSearchButton.Enabled = false;
            itemComboBox.Enabled = false;
            exportSelectedItemButton.Enabled = false;
            exportAllButton.Enabled = false;

            // Set initial placeholder texts for the new labels
            itemNameLabel.Text = "Name: ---";
            itemIdLabel.Text = "ID: ---";
        }

        // FIX: Set 'sender' to nullable (object?)
        private void browseButton_Click(object? sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XML Files (*.xml)|*.xml";
            // UPDATE: Generalize the dialog title to allow any item XML file
            openFileDialog.Title = "Select an FFXI Item XML File";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                currentFilePath = openFileDialog.FileName;
                filePathLabel.Text = $"Loaded: {Path.GetFileName(currentFilePath)}";
                LoadItemsFromXml(currentFilePath);
            }
        }

        private void LoadItemsFromXml(string filePath)
        {
            allItems.Clear(); // Clear any previously loaded items
            try
            {
                if (!File.Exists(filePath))
                {
                    // UPDATE: Change error message to reflect the ability to load any item file
                    MessageBox.Show($"Error: The selected file '{Path.GetFileName(filePath)}' was not found.", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                XDocument doc = XDocument.Load(filePath);
                // Use Descendants to find 'thing' elements with type='Item' regardless of where they are in the XML structure
                var thingElements = doc.Descendants("thing").Where(t => t.Attribute("type")?.Value == "Item");

                foreach (var element in thingElements)
                {
                    var item = new Item();

                    // Find all top-level fields
                    var fields = element.Elements("field");

                    foreach (var field in fields)
                    {
                        string? name = field.Attribute("name")?.Value;
                        string? value = field.Value;

                        // Handle the image field, which is nested within the icon field
                        if (name == "icon")
                        {
                            var imageElement = field.Descendants("field")
                                .FirstOrDefault(f => f.Attribute("name")?.Value == "image");

                            if (imageElement != null && !string.IsNullOrEmpty(imageElement.Value))
                            {
                                try
                                {
                                    byte[] imageBytes = Convert.FromBase64String(imageElement.Value);
                                    using (MemoryStream ms = new MemoryStream(imageBytes))
                                    {
                                        item.Image = new Bitmap(ms);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error decoding image for item: {item.Name} - {ex.Message}");
                                    // Assign null here, which is fine since Image is Bitmap?
                                    item.Image = null;
                                }
                            }
                        }
                        else if (!string.IsNullOrEmpty(name))
                        {
                            // Add all other fields to the attributes dictionary
                            item.Attributes[name] = value ?? string.Empty;
                        }
                    }

                    // Extract name and id for easy access
                    item.Id = item.Attributes.ContainsKey("id") ? item.Attributes["id"] : "N/A";
                    // item.Name is used for the ComboBox DisplayMember
                    item.Name = item.Attributes.ContainsKey("name") ? item.Attributes["name"] : "N/A";
                    item.Description = item.Attributes.ContainsKey("description") ? item.Attributes["description"] : "No description.";

                    allItems.Add(item);
                }

                // Filter out items with a name of "."
                var filteredItems = allItems.Where(item => item.Name != ".").ToList();
                PopulateComboBox(filteredItems);

                // Automatically select the first item if the list is not empty
                if (filteredItems.Any())
                {
                    itemComboBox.SelectedIndex = 0;
                    // FIX: Explicitly call DisplayItemDetails to ensure the UI updates 
                    // regardless of whether the SelectedIndexChanged event fires.
                    DisplayItemDetails(filteredItems[0]);
                }

                // Enable controls after successful load
                searchTextBox.Enabled = true;
                searchButton.Enabled = true;
                clearSearchButton.Enabled = true;
                itemComboBox.Enabled = true;
                exportSelectedItemButton.Enabled = true;
                exportAllButton.Enabled = true;

                DisplayMessage($"Successfully loaded {filteredItems.Count} usable items.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading or parsing XML file: {ex.Message}", "XML Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Disable controls on error
                searchTextBox.Enabled = false;
                searchButton.Enabled = false;
                clearSearchButton.Enabled = false;
                itemComboBox.Enabled = false;
                exportSelectedItemButton.Enabled = false;
                exportAllButton.Enabled = false;
                filePathLabel.Text = "No file loaded.";
                allItems.Clear();
                PopulateComboBox(new List<Item>()); // Reset labels and image
            }
        }

        private void PopulateComboBox(List<Item> itemsToDisplay)
        {
            itemComboBox.DataSource = null; // Clear existing data source
            itemComboBox.DataSource = itemsToDisplay;
            itemComboBox.DisplayMember = "Name"; // Display item names in the dropdown
            itemComboBox.ValueMember = "Id"; // Use item ID as the value

            if (!itemsToDisplay.Any())
            {
                // Reset labels and image if the list is empty
                itemNameLabel.Text = "Name: ---";
                itemIdLabel.Text = "ID: ---";
                itemPictureBox.Image = null;
            }
        }

        // NEW: Event handler for the Clear button
        // FIX: Set 'sender' to nullable (object?)
        private void clearSearchButton_Click(object? sender, EventArgs e)
        {
            searchTextBox.Clear();
            // Trigger the search logic which will now run with an empty string,
            // effectively resetting the item list to show all loaded items.
            searchButton_Click(null, EventArgs.Empty);
        }

        // FIX: Set 'sender' to nullable (object?)
        private void searchButton_Click(object? sender, EventArgs e)
        {
            string searchText = searchTextBox.Text.Trim();

            // Declare the list variable once outside the if/else block
            List<Item> filteredItems;

            if (string.IsNullOrEmpty(searchText))
            {
                // If search box is empty, show all items again, but filtered
                filteredItems = allItems.Where(item => item.Name != ".").ToList();
                DisplayMessage("Please enter an item name or ID to search.");
            }
            else
            {
                // Find all items by name or ID, including the filtering for "."
                filteredItems = allItems.Where(item =>
                    item.Name != "." &&
                    (item.Id.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                ).ToList();
                DisplayMessage($"Found {filteredItems.Count} items matching '{searchText}'.");
            }

            if (filteredItems.Any())
            {
                PopulateComboBox(filteredItems);
                // Select the first item in the filtered list automatically
                itemComboBox.SelectedIndex = 0;
                // Explicitly display the details after search/filter
                DisplayItemDetails(filteredItems[0]);
            }
            else
            {
                // If no items are found, clear the combobox and display a message
                PopulateComboBox(new List<Item>());
            }
        }

        // FIX: Set 'sender' to nullable (object?)
        private void themeToggleCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            if (themeToggleCheckBox.Checked)
            {
                SetTheme(Theme.Dark);
            }
            else
            {
                SetTheme(Theme.Light);
            }
        }

        private void SetTheme(Theme theme)
        {
            // Set all controls to the selected theme
            Color primaryButtonColor = (theme == Theme.Dark) ? Color.FromArgb(255, 140, 0) : Color.FromArgb(120, 70, 180);
            Color primaryTextColor = (theme == Theme.Dark) ? Color.Black : Color.White;
            Color backColor = (theme == Theme.Dark) ? Color.FromArgb(30, 30, 30) : Color.FromArgb(240, 240, 240);
            Color foreColor = (theme == Theme.Dark) ? Color.FromArgb(200, 200, 200) : Color.FromArgb(30, 30, 30);
            Color inputBackColor = (theme == Theme.Dark) ? Color.FromArgb(50, 50, 50) : Color.FromArgb(250, 250, 250);

            this.BackColor = backColor;
            this.ForeColor = foreColor;

            this.searchTextBox.BackColor = inputBackColor;
            this.searchTextBox.ForeColor = foreColor;

            this.itemComboBox.BackColor = inputBackColor;
            this.itemComboBox.ForeColor = foreColor;

            this.itemDetailsTextBox.BackColor = (theme == Theme.Dark) ? Color.FromArgb(50, 50, 50) : Color.FromArgb(255, 255, 224);
            this.itemDetailsTextBox.ForeColor = foreColor;

            this.searchButton.BackColor = primaryButtonColor;
            this.browseButton.BackColor = primaryButtonColor;
            this.exportSelectedItemButton.BackColor = primaryButtonColor;
            this.exportAllButton.BackColor = primaryButtonColor;
            // NEW: Set color for the clear button
            this.clearSearchButton.BackColor = Color.FromArgb(100, 100, 100); // Neutral grey/darker color

            this.searchButton.ForeColor = primaryTextColor;
            this.browseButton.ForeColor = primaryTextColor;
            this.exportSelectedItemButton.ForeColor = primaryTextColor;
            this.exportAllButton.ForeColor = primaryTextColor;
            this.clearSearchButton.ForeColor = Color.White; // Always white for the clear button

            // Set colors for the new labels
            this.itemNameLabel.ForeColor = foreColor;
            this.itemIdLabel.ForeColor = foreColor;

            this.Invalidate();
        }

        // FIX: Set 'sender' to nullable (object?)
        private void itemComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // Use 'as Item' and null check for safe casting
            if (itemComboBox.SelectedItem is Item selectedItem)
            {
                DisplayItemDetails(selectedItem);
            }
        }

        // This method handles the custom drawing of each item in the ComboBox dropdown
        // FIX: Set 'sender' to nullable (object?)
        private void itemComboBox_DrawItem(object? sender, DrawItemEventArgs e)
        {
            // Ensure there is a valid item to draw
            if (e.Index < 0) return;

            // Get the item to draw from the ComboBox
            Item? item = itemComboBox.Items[e.Index] as Item;

            if (item == null) return;

            // Draw the background of the item, handling selection highlight
            e.DrawBackground();

            // Draw the bitmap if it exists
            if (item.Image != null)
            {
                // Create a rectangle for the image (sized to 32x32 with a small margin)
                Rectangle imageRect = new Rectangle(e.Bounds.Left, e.Bounds.Top, 32, 32);
                e.Graphics.DrawImage(item.Image, imageRect);
            }

            // Draw the item's name
            // Determine the text color based on if the item is selected
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                // FIX: Use null-forgiving operator (!) on e.Font to resolve potential null warning
                e.Graphics.DrawString(item.Name, e.Font!, SystemBrushes.HighlightText, e.Bounds.Left + 35, e.Bounds.Top);
            }
            else
            {
                using (Brush textBrush = new SolidBrush(itemComboBox.ForeColor))
                {
                    // FIX: Use null-forgiving operator (!) on e.Font to resolve potential null warning
                    e.Graphics.DrawString(item.Name, e.Font!, textBrush, e.Bounds.Left + 35, e.Bounds.Top);
                }
            }

            // Draw the focus rectangle if the item has focus
            e.DrawFocusRectangle();
        }

        private void DisplayItemDetails(Item item)
        {
            // Logic to use "log-name-singular" as the primary display name
            string displayName = item.Attributes.ContainsKey("log-name-singular")
                ? item.Attributes["log-name-singular"]
                : item.Name;

            // Set the Name and ID labels
            itemNameLabel.Text = $"Name: {displayName}";
            itemIdLabel.Text = $"ID: {item.Id}";

            // Clear the existing text and set the image
            itemDetailsTextBox.Clear();
            itemDetailsTextBox.SelectionIndent = 15; // Add left padding to the text
            itemPictureBox.Image = item.Image;

            // Build and display the fixed text, using the preferred displayName
            itemDetailsTextBox.AppendText($"Name: {displayName}\r\n");
            itemDetailsTextBox.AppendText($"ID: {item.Id}\r\n");

            // Display the full description string
            itemDetailsTextBox.AppendText($"Description: {item.Description}\r\n");

            // Add attributes
            itemDetailsTextBox.AppendText("\r\n--- Attributes ---\r\n");

            // Save the default color to reset it later
            Color originalColor = itemDetailsTextBox.ForeColor;

            foreach (var attribute in item.Attributes)
            {
                // Skip the "description" attribute as it's already displayed in the main section
                if (attribute.Key == "description")
                {
                    continue;
                }

                // Special handling for the "skill" attribute
                if (attribute.Key == "skill" && int.TryParse(attribute.Value, System.Globalization.NumberStyles.HexNumber, null, out int skillValue))
                {
                    if (ItemData.skillData.TryGetValue(skillValue, out string? skillName))
                    {
                        itemDetailsTextBox.AppendText($"{attribute.Key}: {attribute.Value} ({skillName})\r\n");
                    }
                    else
                    {
                        // If the skill value is not found, just print the raw value
                        itemDetailsTextBox.AppendText($"{attribute.Key}: {attribute.Value}\r\n");
                    }
                    continue;
                }

                // Special handling for the "element" attribute
                if (attribute.Key == "element" && !string.IsNullOrEmpty(attribute.Value))
                {
                    if (int.TryParse(attribute.Value, System.Globalization.NumberStyles.HexNumber, null, out int decimalValue) && ItemData.elementData.ContainsKey(decimalValue))
                    {
                        var elementInfo = ItemData.elementData[decimalValue];
                        // Append the attribute line up to the name
                        itemDetailsTextBox.AppendText($"{attribute.Key}: {attribute.Value} (");

                        // Set the color for the element name
                        itemDetailsTextBox.SelectionStart = itemDetailsTextBox.Text.Length;
                        itemDetailsTextBox.SelectionLength = elementInfo.Name.Length;
                        itemDetailsTextBox.SelectionColor = elementInfo.Color;
                        itemDetailsTextBox.AppendText(elementInfo.Name);

                        // Reset the color and append the closing parenthesis and newline
                        itemDetailsTextBox.SelectionColor = originalColor;
                        itemDetailsTextBox.AppendText(")\r\n");
                        continue; // Move to the next attribute
                    }
                }

                // Special handling for the "slots" attribute
                if (attribute.Key == "slots" && !string.IsNullOrEmpty(attribute.Value))
                {
                    if (int.TryParse(attribute.Value, System.Globalization.NumberStyles.HexNumber, null, out int slotValue))
                    {
                        if (ItemData.slotData.TryGetValue(slotValue, out string? slotName))
                        {
                            itemDetailsTextBox.AppendText($"{attribute.Key}: {attribute.Value} ({slotName})\r\n");
                        }
                        else
                        {
                            // If the slot value is not found, just print the raw value
                            itemDetailsTextBox.AppendText($"{attribute.Key}: {attribute.Value}\r\n");
                        }
                        continue;
                    }
                }

                // Special handling for the "flags" attribute
                if (attribute.Key == "flags" && int.TryParse(attribute.Value, System.Globalization.NumberStyles.HexNumber, null, out int flagValue))
                {
                    // Add the flag value in both hex and decimal formats
                    itemDetailsTextBox.AppendText($"{attribute.Key}: {attribute.Value} (Decimal: {flagValue})\r\n");
                    itemDetailsTextBox.AppendText("  -- Flags Detail --\r\n");

                    // Iterate through the list of flags to check which ones are set
                    foreach (var flag in ItemData.hexFlags)
                    {
                        if ((flagValue & flag.Value) != 0)
                        {
                            itemDetailsTextBox.AppendText($"  * {flag.Name}\r\n");
                        }
                    }
                    continue; // Move to the next attribute
                }

                // Special handling for the "jobs" attribute
                if (attribute.Key == "jobs" && !string.IsNullOrEmpty(attribute.Value))
                {
                    if (int.TryParse(attribute.Value, System.Globalization.NumberStyles.HexNumber, null, out int jobValue))
                    {
                        // Get the list of enabled job names
                        var enabledJobs = GetEnabledJobs(jobValue);

                        // Calculate the combined LSB value from the enabled jobs
                        int lsbBitmask = CalculateCombinedLsbBitmask(enabledJobs);

                        // Append the job list as a comma-separated string
                        itemDetailsTextBox.AppendText($"{attribute.Key}: {attribute.Value} (Combined Hex Value: {jobValue})\r\n");
                        itemDetailsTextBox.AppendText($"  -- Enabled Jobs: {string.Join("/", enabledJobs)}\r\n");

                        // Append the calculated LSB value
                        itemDetailsTextBox.AppendText($"  -- Combined LSB Bitmask: {lsbBitmask}\r\n");
                    }
                    continue;
                }

                string attributeText = $"{attribute.Key}: {attribute.Value}\r\n";
                itemDetailsTextBox.AppendText(attributeText);
            }
            // Call ResizeControls to handle dynamic font sizing and scrollbar visibility
            ResizeControls();
        }

        // Method to handle the Export Selected Item button click
        // FIX: Set 'sender' to nullable (object?)
        private void exportSelectedItemButton_Click(object? sender, EventArgs e)
        {
            if (itemComboBox.SelectedItem is Item selectedItem)
            {
                // Generate and copy the SQL statement for the selected item
                string sqlStatement = GenerateSqlStatement(selectedItem);

                Clipboard.SetText(sqlStatement);

                // Display the SQL statement to the user with a confirmation
                string message = $"SQL statement copied to clipboard:\n\n{sqlStatement}";
                MessageBox.Show(message, "Exported SQL Statement", MessageBoxButtons.OK);
            }
            else
            {
                MessageBox.Show("Please select an item to export.", "No Item Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Method to handle the Export All button click
        // FIX: Set 'sender' to nullable (object?)
        private void exportAllButton_Click(object? sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "SQL Files (*.sql)|*.sql";
            saveFileDialog.Title = "Save SQL Export File";
            saveFileDialog.FileName = "item_basic_export.sql";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = saveFileDialog.FileName;
                try
                {
                    using (StreamWriter writer = new StreamWriter(filePath))
                    {
                        foreach (var item in allItems)
                        {
                            // Skip items that have a name of "."
                            if (item.Name == ".")
                            {
                                continue;
                            }
                            string sqlStatement = GenerateSqlStatement(item);
                            writer.WriteLine(sqlStatement);
                        }
                    }
                    MessageBox.Show($"Successfully exported {allItems.Count} items to:\n{filePath}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred during export:\n{ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Helper method to generate the SQL statement for a single item
        private string GenerateSqlStatement(Item item)
        {
            // Extract item data and remove apostrophes
            string itemId = item.Id;

            // New logic: 'name' is now 'log-name-singular'
            string name = item.Attributes.ContainsKey("log-name-singular") ?
                item.Attributes["log-name-singular"].ToLower().Replace(" ", "_").Replace("'", "") :
                item.Name.ToLower().Replace(" ", "_").Replace("'", "");

            // New logic: 'shortname' is now 'name'
            string shortname = item.Attributes.ContainsKey("name") ?
                item.Attributes["name"].ToLower().Replace(" ", "_").Replace("'", "") :
                item.Name.ToLower().Replace(" ", "_").Replace("'", "");

            string stackSize = item.Attributes.ContainsKey("stack-size") ?
                item.Attributes["stack-size"] : "1";

            int flagDecimalValue = 0;
            if (item.Attributes.ContainsKey("flags") && int.TryParse(item.Attributes["flags"], System.Globalization.NumberStyles.HexNumber, null, out int flagsValue))
            {
                flagDecimalValue = flagsValue;
            }

            int noSale = 0;
            if (item.Attributes.ContainsKey("flags") && int.TryParse(item.Attributes["flags"], System.Globalization.NumberStyles.HexNumber, null, out int flagValue))
            {
                // FIX: Using the null-forgiving operator on 'Value' since it is being used in a null-coalescing expression
                int notSelableFlagValue = ItemData.hexFlags.FirstOrDefault(f => f.Name == "NOT SELABLE")?.Value ?? 0;
                if ((flagValue & notSelableFlagValue) != 0)
                {
                    noSale = 1;
                }
            }

            // Determine the export term based on the description
            string exportTerm = item.Description.IndexOf("Furnishing", StringComparison.OrdinalIgnoreCase) >= 0
                ? "@FURNISHINGS"
                : "@NONE";

            // Construct the SQL statement with the correct flag value
            return $"INSERT INTO `item_basic` VALUES ({itemId},0,'{name}','{shortname}',{stackSize},{flagDecimalValue},'{exportTerm}',{noSale},0);";
        }

        // Helper method to get a list of enabled jobs from the hex value
        private List<string> GetEnabledJobs(int jobValue)
        {
            var enabledJobs = new List<string>();
            // Use KeyValuePair<string, int> to correctly iterate over the Dictionary
            foreach (KeyValuePair<string, int> job in ItemData.jobHexData)
            {
                if ((jobValue & job.Value) != 0)
                {
                    enabledJobs.Add(job.Key);
                }
            }
            return enabledJobs;
        }

        // Helper method to calculate the combined LSB bitmask from a list of job names
        private int CalculateCombinedLsbBitmask(List<string> enabledJobs)
        {
            int lsbValue = 0;
            foreach (var jobName in enabledJobs)
            {
                if (ItemData.jobLSBData.ContainsKey(jobName))
                {
                    lsbValue += ItemData.jobLSBData[jobName];
                }
            }
            return lsbValue;
        }

        private void ResizeControls()
        {
            string textToMeasure = itemDetailsTextBox.Text;

            // 1. Determine the target font size for short content visibility
            float targetFontSize = MAX_FONT_SIZE;

            // Logic to make short/empty content look less sparse in the large box
            if (textToMeasure.Length < 100)
            {
                targetFontSize = MAX_FONT_SIZE - 2f; // e.g., 8pt
            }
            else if (textToMeasure.Length < 25)
            {
                targetFontSize = MAX_FONT_SIZE - 3f; // e.g., 7pt
            }

            // Ensure the font size is within the allowed bounds (MAX_FONT_SIZE to MIN_FONT_SIZE)
            targetFontSize = Math.Clamp(targetFontSize, MIN_FONT_SIZE, MAX_FONT_SIZE);

            // 2. Apply the new font size
            if (Math.Abs(itemDetailsTextBox.Font.Size - targetFontSize) > 0.01f) // Compare floats safely
            {
                itemDetailsTextBox.Font = new Font(itemDetailsTextBox.Font.FontFamily, targetFontSize, itemDetailsTextBox.Font.Style);
            }

            // 3. Set scrollbars based on content length relative to CURRENT size
            // The RichTextBox is now resized by the form via anchoring. We only need to control scrollbars.
            Font currentFont = itemDetailsTextBox.Font;
            int maxTextBoxWidth = itemDetailsTextBox.ClientSize.Width; // Use ClientSize width for measurement

            // Measure the height required for the current text with wrapping
            Size textSize = TextRenderer.MeasureText(textToMeasure, currentFont, new Size(maxTextBoxWidth - 15, int.MaxValue), TextFormatFlags.WordBreak);

            // Check if the content height exceeds the current TextBox display height
            if (textSize.Height > itemDetailsTextBox.ClientSize.Height)
            {
                itemDetailsTextBox.ScrollBars = RichTextBoxScrollBars.Vertical;
            }
            else
            {
                itemDetailsTextBox.ScrollBars = RichTextBoxScrollBars.None;
            }
        }

        private void DisplayMessage(string message)
        {
            itemDetailsTextBox.Clear();
            itemDetailsTextBox.SelectionIndent = 15; // Add left padding to the text
            itemDetailsTextBox.Text = message;
            ResizeControls();
        }

        // ** FIX: Declared components here to ensure it's defined once for the Dispose method **
        private System.ComponentModel.IContainer components = null!;

        // The following section contains the Windows Forms Designer components.
        // FIX: Use null-forgiving operator for all designer-managed controls
        private System.Windows.Forms.TextBox searchTextBox = null!;
        private System.Windows.Forms.Button searchButton = null!;
        private System.Windows.Forms.Button clearSearchButton = null!;
        private System.Windows.Forms.ComboBox itemComboBox = null!;
        private System.Windows.Forms.RichTextBox itemDetailsTextBox = null!; // Changed to RichTextBox
        private System.Windows.Forms.Button browseButton = null!;
        private System.Windows.Forms.Button exportSelectedItemButton = null!;
        private System.Windows.Forms.Button exportAllButton = null!;
        private System.Windows.Forms.Label filePathLabel = null!;
        private System.Windows.Forms.PictureBox itemPictureBox = null!;
        private System.Windows.Forms.Label titleLabel = null!;
        private System.Windows.Forms.CheckBox themeToggleCheckBox = null!;
        private System.Windows.Forms.Label itemIdLabel = null!;
        private System.Windows.Forms.Label itemNameLabel = null!;

        private void InitializeComponent()
        {
            // Initialization: Create the component container
            this.components = new System.ComponentModel.Container();
            this.searchTextBox = new System.Windows.Forms.TextBox();
            this.searchButton = new System.Windows.Forms.Button();
            this.clearSearchButton = new System.Windows.Forms.Button();
            this.itemComboBox = new System.Windows.Forms.ComboBox();
            this.itemDetailsTextBox = new System.Windows.Forms.RichTextBox();
            this.browseButton = new System.Windows.Forms.Button();
            this.exportSelectedItemButton = new System.Windows.Forms.Button();
            this.exportAllButton = new System.Windows.Forms.Button();
            this.filePathLabel = new System.Windows.Forms.Label();
            this.itemPictureBox = new System.Windows.Forms.PictureBox();
            this.titleLabel = new System.Windows.Forms.Label();
            this.themeToggleCheckBox = new System.Windows.Forms.CheckBox();
            this.itemIdLabel = new System.Windows.Forms.Label();
            this.itemNameLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // Form1
            //
            this.Icon = new Icon("F:\\Item Viewer and Editor\\Item Viewer and Editor\\Item Viewer and Editor\\Item_viewer_icon_v2.ico");
            this.ClientSize = new System.Drawing.Size(895, 455);
            this.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
            this.ForeColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this.Font = new System.Drawing.Font("Segoe UI", MAX_FONT_SIZE, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MinimumSize = new System.Drawing.Size(800, 450); // Set minimum size to prevent layout breaking
            //
            // titleLabel
            //
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleLabel.Location = new System.Drawing.Point(12, 10);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(200, 30);
            this.titleLabel.TabIndex = 7;
            this.titleLabel.Text = "FFXI ITEM VIEWER";
            //
            // themeToggleCheckBox
            //
            // ANCHOR: Now anchored to Top | Right
            this.themeToggleCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.themeToggleCheckBox.AutoSize = true;
            this.themeToggleCheckBox.Location = new System.Drawing.Point(750, 12);
            this.themeToggleCheckBox.Name = "themeToggleCheckBox";
            this.themeToggleCheckBox.Size = new System.Drawing.Size(120, 25);
            this.themeToggleCheckBox.TabIndex = 8;
            this.themeToggleCheckBox.Text = "Dark Theme";
            this.themeToggleCheckBox.UseVisualStyleBackColor = true;
            //
            // browseButton
            //
            this.browseButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
            this.browseButton.BackColor = System.Drawing.Color.FromArgb(120, 70, 180);
            this.browseButton.ForeColor = System.Drawing.Color.White;
            this.browseButton.Location = new System.Drawing.Point(12, 50);
            this.browseButton.Name = "browseButton";
            this.browseButton.Size = new System.Drawing.Size(90, 29);
            this.browseButton.TabIndex = 4;
            this.browseButton.Text = "Browse...";
            this.browseButton.FlatStyle = FlatStyle.Flat;
            this.browseButton.FlatAppearance.BorderSize = 0;
            this.browseButton.UseVisualStyleBackColor = false;
            //
            // exportSelectedItemButton
            //
            // ANCHOR: Top | Right
            this.exportSelectedItemButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.exportSelectedItemButton.BackColor = System.Drawing.Color.FromArgb(120, 70, 180);
            this.exportSelectedItemButton.ForeColor = System.Drawing.Color.White;
            this.exportSelectedItemButton.Location = new System.Drawing.Point(662, 50);
            this.exportSelectedItemButton.Name = "exportSelectedItemButton";
            this.exportSelectedItemButton.Size = new System.Drawing.Size(220, 29);
            this.exportSelectedItemButton.TabIndex = 9;
            this.exportSelectedItemButton.Text = "Export Item For items_basic.sql";
            this.exportSelectedItemButton.FlatStyle = FlatStyle.Flat;
            this.exportSelectedItemButton.FlatAppearance.BorderSize = 0;
            this.exportSelectedItemButton.UseVisualStyleBackColor = false;
            //
            // exportAllButton
            //
            // ANCHOR: Top | Right
            this.exportAllButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.exportAllButton.BackColor = System.Drawing.Color.FromArgb(120, 70, 180);
            this.exportAllButton.ForeColor = System.Drawing.Color.White;
            this.exportAllButton.Location = new System.Drawing.Point(702, 90);
            this.exportAllButton.Name = "exportAllButton";
            this.exportAllButton.Size = new System.Drawing.Size(180, 29);
            this.exportAllButton.TabIndex = 10;
            this.exportAllButton.Text = "EXPORT ALL TO SQL FILE";
            this.exportAllButton.FlatStyle = FlatStyle.Flat;
            this.exportAllButton.FlatAppearance.BorderSize = 0;
            this.exportAllButton.UseVisualStyleBackColor = false;
            //
            // filePathLabel
            //
            this.filePathLabel.AutoSize = true;
            this.filePathLabel.Location = new System.Drawing.Point(108, 55);
            this.filePathLabel.Name = "filePathLabel";
            this.filePathLabel.Size = new System.Drawing.Size(91, 13);
            this.filePathLabel.TabIndex = 5;
            this.filePathLabel.Text = "No file loaded.";
            //
            // itemNameLabel
            //
            this.itemNameLabel.AutoSize = true;
            this.itemNameLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.itemNameLabel.Location = new System.Drawing.Point(12, 85);
            this.itemNameLabel.Name = "itemNameLabel";
            this.itemNameLabel.Size = new System.Drawing.Size(100, 21);
            this.itemNameLabel.TabIndex = 12;
            this.itemNameLabel.Text = "Name: ---";
            //
            // itemIdLabel
            //
            this.itemIdLabel.AutoSize = true;
            this.itemIdLabel.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.itemIdLabel.Location = new System.Drawing.Point(12, 110);
            this.itemIdLabel.Name = "itemIdLabel";
            this.itemIdLabel.Size = new System.Drawing.Size(40, 17);
            this.itemIdLabel.TabIndex = 11;
            this.itemIdLabel.Text = "ID: ---";
            //
            // itemPictureBox
            //
            this.itemPictureBox.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.itemPictureBox.Location = new System.Drawing.Point(418, 50);
            this.itemPictureBox.Name = "itemPictureBox";
            this.itemPictureBox.Size = new System.Drawing.Size(64, 64); // Set size appropriately
            this.itemPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            this.itemPictureBox.TabIndex = 6;
            this.itemPictureBox.TabStop = false;
            //
            // searchTextBox
            //
            // Anchor to Top | Left, fixed width
            this.searchTextBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
            this.searchTextBox.BackColor = System.Drawing.Color.FromArgb(250, 250, 250);
            this.searchTextBox.ForeColor = System.Drawing.Color.Black;
            this.searchTextBox.Location = new System.Drawing.Point(12, 135);
            this.searchTextBox.Name = "searchTextBox";
            this.searchTextBox.Size = new System.Drawing.Size(430, 29);
            this.searchTextBox.TabIndex = 0;
            //
            // searchButton
            //
            // Anchor to Top | Left, positioned next to the search box
            this.searchButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
            this.searchButton.BackColor = System.Drawing.Color.FromArgb(120, 70, 180);
            this.searchButton.ForeColor = System.Drawing.Color.White;
            this.searchButton.Location = new System.Drawing.Point(450, 135);
            this.searchButton.Name = "searchButton";
            this.searchButton.Size = new System.Drawing.Size(90, 29);
            this.searchButton.TabIndex = 1;
            this.searchButton.Text = "Search";
            this.searchButton.FlatStyle = FlatStyle.Flat;
            this.searchButton.FlatAppearance.BorderSize = 0;
            this.searchButton.UseVisualStyleBackColor = false;
            //
            // clearSearchButton
            //
            // Clear Search Button
            this.clearSearchButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
            this.clearSearchButton.BackColor = System.Drawing.Color.FromArgb(100, 100, 100);
            this.clearSearchButton.ForeColor = System.Drawing.Color.White;
            this.clearSearchButton.Location = new System.Drawing.Point(548, 135); // Positioned 8px to the right of searchButton
            this.clearSearchButton.Name = "clearSearchButton";
            this.clearSearchButton.Size = new System.Drawing.Size(90, 29);
            this.clearSearchButton.TabIndex = 13;
            this.clearSearchButton.Text = "Clear";
            this.clearSearchButton.FlatStyle = FlatStyle.Flat;
            this.clearSearchButton.FlatAppearance.BorderSize = 0;
            this.clearSearchButton.UseVisualStyleBackColor = false;
            //
            // itemComboBox
            //
            this.itemComboBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.itemComboBox.BackColor = System.Drawing.Color.FromArgb(250, 250, 250);
            this.itemComboBox.ForeColor = Color.Black;
            this.itemComboBox.FormattingEnabled = true;
            this.itemComboBox.Location = new System.Drawing.Point(12, 180);
            this.itemComboBox.Name = "itemComboBox";
            this.itemComboBox.Size = new System.Drawing.Size(870, 29);
            this.itemComboBox.TabIndex = 2;
            this.itemComboBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.itemComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.itemComboBox.ItemHeight = 32; // Set the item height to accommodate the image
            //
            // itemDetailsTextBox
            //
            // Anchor to Left, Right, Top, and Bottom for fluid resizing
            this.itemDetailsTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.itemDetailsTextBox.BackColor = System.Drawing.Color.FromArgb(255, 255, 224);
            this.itemDetailsTextBox.ForeColor = System.Drawing.Color.Black;
            this.itemDetailsTextBox.Location = new System.Drawing.Point(12, 225);
            this.itemDetailsTextBox.Multiline = true;
            this.itemDetailsTextBox.Name = "itemDetailsTextBox";
            this.itemDetailsTextBox.ReadOnly = true;
            this.itemDetailsTextBox.ScrollBars = RichTextBoxScrollBars.None; // Will be set dynamically
            this.itemDetailsTextBox.Size = new System.Drawing.Size(870, 215);
            this.itemDetailsTextBox.TabIndex = 3;
            //
            // Form1
            //
            this.ClientSize = new System.Drawing.Size(895, 455);
            this.Controls.Add(this.clearSearchButton);
            this.Controls.Add(this.itemNameLabel);
            this.Controls.Add(this.itemIdLabel);
            this.Controls.Add(this.themeToggleCheckBox);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.itemPictureBox);
            this.Controls.Add(this.filePathLabel);
            this.Controls.Add(this.browseButton);
            this.Controls.Add(this.exportSelectedItemButton);
            this.Controls.Add(this.exportAllButton);
            this.Controls.Add(this.itemDetailsTextBox);
            this.Controls.Add(this.itemComboBox);
            this.Controls.Add(this.searchButton);
            this.Controls.Add(this.searchTextBox);
            this.Name = "Form1";
            this.Text = "FFXI Item Viewer";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
