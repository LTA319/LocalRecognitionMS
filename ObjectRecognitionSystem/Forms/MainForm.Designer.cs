namespace ObjectRecognitionSystem.Forms;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;
    private PictureBox pbPreview = null!;
    private Button btnCapture = null!;
    private CheckBox cbAutoDetect = null!;
    private Label lblName = null!;
    private Label lblConfidence = null!;
    private Label lblStatus = null!;
    private DataGridView dgvCandidates = null!;
    private Button btnFeedback = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.pbPreview = new PictureBox();
        this.btnCapture = new Button();
        this.cbAutoDetect = new CheckBox();
        this.lblName = new Label();
        this.lblConfidence = new Label();
        this.lblStatus = new Label();
        this.dgvCandidates = new DataGridView();
        this.btnFeedback = new Button();
        ((System.ComponentModel.ISupportInitialize)(this.pbPreview)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.dgvCandidates)).BeginInit();
        this.SuspendLayout();

        // pbPreview
        this.pbPreview.BorderStyle = BorderStyle.FixedSingle;
        this.pbPreview.Location = new Point(12, 12);
        this.pbPreview.Size = new Size(640, 480);
        this.pbPreview.SizeMode = PictureBoxSizeMode.Zoom;
        this.pbPreview.TabIndex = 0;
        this.pbPreview.TabStop = false;

        // btnCapture
        this.btnCapture.Location = new Point(12, 500);
        this.btnCapture.Size = new Size(100, 36);
        this.btnCapture.TabIndex = 1;
        this.btnCapture.Text = "识别";

        // cbAutoDetect
        this.cbAutoDetect.AutoSize = true;
        this.cbAutoDetect.Location = new Point(120, 508);
        this.cbAutoDetect.Size = new Size(90, 21);
        this.cbAutoDetect.TabIndex = 2;
        this.cbAutoDetect.Text = "自动识别";
        this.cbAutoDetect.UseVisualStyleBackColor = true;

        // lblName
        this.lblName.AutoSize = true;
        this.lblName.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold, GraphicsUnit.Point);
        this.lblName.Location = new Point(670, 20);
        this.lblName.Size = new Size(120, 25);
        this.lblName.TabIndex = 3;
        this.lblName.Text = "物品名称";

        // lblConfidence
        this.lblConfidence.AutoSize = true;
        this.lblConfidence.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        this.lblConfidence.Location = new Point(670, 55);
        this.lblConfidence.Size = new Size(50, 20);
        this.lblConfidence.TabIndex = 4;

        // lblStatus
        this.lblStatus.AutoSize = true;
        this.lblStatus.ForeColor = Color.Gray;
        this.lblStatus.Location = new Point(670, 85);
        this.lblStatus.Size = new Size(200, 17);
        this.lblStatus.TabIndex = 5;

        // dgvCandidates
        this.dgvCandidates.AllowUserToAddRows = false;
        this.dgvCandidates.AllowUserToDeleteRows = false;
        this.dgvCandidates.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.dgvCandidates.Location = new Point(670, 120);
        this.dgvCandidates.Size = new Size(400, 200);
        this.dgvCandidates.TabIndex = 6;
        this.dgvCandidates.ReadOnly = true;
        this.dgvCandidates.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        this.dgvCandidates.MultiSelect = false;

        // btnFeedback
        this.btnFeedback.Location = new Point(670, 330);
        this.btnFeedback.Size = new Size(120, 30);
        this.btnFeedback.TabIndex = 7;
        this.btnFeedback.Text = "纠正映射";

        // MainForm
        this.AutoScaleDimensions = new SizeF(7F, 17F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(1090, 550);
        this.Controls.Add(this.btnFeedback);
        this.Controls.Add(this.dgvCandidates);
        this.Controls.Add(this.lblStatus);
        this.Controls.Add(this.lblConfidence);
        this.Controls.Add(this.lblName);
        this.Controls.Add(this.cbAutoDetect);
        this.Controls.Add(this.btnCapture);
        this.Controls.Add(this.pbPreview);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.Name = "MainForm";
        this.Text = "物料识别系统";
        ((System.ComponentModel.ISupportInitialize)(this.pbPreview)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.dgvCandidates)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
