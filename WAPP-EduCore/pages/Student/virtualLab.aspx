<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="virtualLab.aspx.cs" Inherits="WAPPEduCore.virtualLab" Async="true" %>
<%@ Register Src="~/pages/Shared/Sidebar.ascx" TagName="Sidebar" TagPrefix="uc" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Merriweather:wght@400;700&display=swap" rel="stylesheet" />
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/student.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/student-upgrade.css" rel="stylesheet" type="text/css" />
    <title>EduCore - Unified Science Virtual Lab</title>
<style>
        :root {
            --bg-page: #faf8f5;
            --nav-bg: #0f1b45;
            --ink: #0f1a3c;
            --text: #4a5163;
            --muted: #7b8194;
            --border: #e1e4ea;
            --card: #ffffff;
            --accent: #4a5fc8;
            --accent-link: #2f4fc0;
            --btn: #2a4494;
            --btn-hover: #1f3377;
            --serif: 'Source Serif 4', 'Source Serif Pro', Georgia, serif;
        }

        html, body {
            height: 100%;
            margin: 0;
            padding: 0;
            background-color: var(--bg-page);
            color: var(--ink);
        }

        #form1 {
            display: flex;
            min-height: 100vh;
            width: 100vw;
            overflow-x: hidden;
        }

        /* Main content offset to match other student pages and clear the sidebar */
        .main-content {
            flex: 1;
            margin-left: 260px;
            display: flex;
            flex-direction: column;
            overflow-y: auto;
            padding: 40px;
            min-height: 100vh;
            box-sizing: border-box;
            background-color: var(--bg-page);
        }

        .header-top { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }
        .header-title { font-size: 24px; font-weight: 600; color: var(--ink); display: flex; align-items: center; gap: 12px; }
        
        .lab-tabs { display: flex; gap: 10px; margin-bottom: 24px; border-bottom: 2px solid var(--border); padding-bottom: 12px; }
        .tab-btn {
            background: var(--card);
            border: 1px solid var(--border);
            padding: 8px 16px;
            border-radius: 6px;
            font-weight: 600;
            cursor: pointer;
            color: var(--muted);
            transition: all 0.2s;
            font-size: 13px;
        }
        .tab-btn.active-tab {
            background-color: var(--accent);
            color: white;
            border-color: var(--accent);
        }

        .workspace {
            display: grid;
            grid-template-columns: 2fr 1fr;
            gap: 24px;
        }

        .card {
            background: var(--card);
            border: 1px solid var(--border);
            border-radius: 10px;
            padding: 20px;
            box-shadow: 0 6px 18px rgba(16,24,40,0.04);
            margin-bottom: 20px;
        }

        .card h3 {
            font-size: 17px;
            font-weight: 600;
            color: var(--ink);
            margin-bottom: 12px;
        }

        .sim-canvas {
            background: #0f1b45;
            border-radius: 8px;
            height: 420px;
            display: flex;
            align-items: center;
            justify-content: center;
            color: #c3c9de;
            font-size: 14px;
            position: relative;
        }

        .control-group { margin-bottom: 16px; }
        .control-group label { display: block; font-size: 13px; color: var(--text); margin-bottom: 6px; font-weight: 500; }
        .form-control { width: 100%; padding: 10px; border: 1px solid var(--border); border-radius: 6px; box-sizing: border-box; background-color: #fff; color: var(--ink); font-size: 14px; }
        
        .btn-primary {
            background-color: var(--btn);
            color: white;
            border: none;
            padding: 10px 16px;
            width: 100%;
            border-radius: 6px;
            font-weight: 600;
            font-size: 13px;
            cursor: pointer;
            transition: background-color 0.2s;
        }
        .btn-primary:hover { background-color: var(--btn-hover); }
    </style>

    <!-- Enhanced Canvas Graph Plotting with Proper Axis Labels -->
    <script type="text/javascript">
function drawSimulationGraph(points, xLabel, yLabel) {
    var canvas = document.getElementById('scienceCanvas');
    if (!canvas) return;
    var ctx = canvas.getContext('2d');

    // Clear canvas
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    if (!points || points.length === 0) return;

    var padding = 55;
    var w = canvas.width - (padding + 30);
    var h = canvas.height - (padding + 30);

    // Find max bounds for scaling
    var maxX = Math.max(...points.map(p => p.x)) || 10;
    var rawMaxY = Math.max(...points.map(p => p.y)) || 10;
    var maxY = rawMaxY * 1.15;

    // Draw Graph Title
    ctx.fillStyle = '#ffffff';
    ctx.font = 'bold 13px Segoe UI';
    ctx.textAlign = 'center';
    ctx.fillText(yLabel + " vs " + xLabel, canvas.width / 2, 22);

    // Draw Axes lines
    ctx.strokeStyle = '#475569';
    ctx.lineWidth = 1.5;
    ctx.beginPath();
    ctx.moveTo(padding, canvas.height - padding);
    ctx.lineTo(canvas.width - 20, canvas.height - padding); // X axis
    ctx.moveTo(padding, canvas.height - padding);
    ctx.lineTo(padding, 35); // Y axis
    ctx.stroke();

    // Draw Numeric Scale / Ticks (Y-Axis)
    ctx.fillStyle = '#94a3b8';
    ctx.font = '10px Segoe UI';
    ctx.textAlign = 'right';
    var yTicks = 5;
    for (let i = 0; i <= yTicks; i++) {
        var val = (maxY / yTicks) * i;
        var yPos = (canvas.height - padding) - (h / yTicks) * i;
        ctx.fillText(val.toFixed(1), padding - 8, yPos + 4);

        // Small tick mark
        ctx.beginPath();
        ctx.moveTo(padding - 3, yPos);
        ctx.lineTo(padding, yPos);
        ctx.stroke();
    }

    // Draw Numeric Scale / Ticks (X-Axis)
    ctx.textAlign = 'center';
    var xTicks = 5;
    for (let i = 0; i <= xTicks; i++) {
        var val = (maxX / xTicks) * i;
        var xPos = padding + (w / xTicks) * i;
        ctx.fillText(val.toFixed(1), xPos, canvas.height - padding + 15);

        // Small tick mark
        ctx.beginPath();
        ctx.moveTo(xPos, canvas.height - padding);
        ctx.lineTo(xPos, canvas.height - padding + 3);
        ctx.stroke();
    }

    // Axis Labels
    ctx.fillStyle = '#cbd5e1';
    ctx.font = '12px Segoe UI';
    ctx.textAlign = 'center';
    ctx.fillText(xLabel || 'X-Axis', padding + (w / 2), canvas.height - 8);

    ctx.save();
    ctx.translate(16, canvas.height / 2);
    ctx.rotate(-Math.PI / 2);
    ctx.fillText(yLabel || 'Y-Axis', 0, 0);
    ctx.restore();

    // Plot Trajectory Curve
    ctx.strokeStyle = '#3b82f6';
    ctx.lineWidth = 3;
    ctx.beginPath();

    points.forEach((pt, index) => {
        var cx = padding + (pt.x / maxX) * w;
        var cy = (canvas.height - padding) - (pt.y / maxY) * h;

        if (index === 0) ctx.moveTo(cx, cy);
        else ctx.lineTo(cx, cy);
    });
    ctx.stroke();

    // Draw Data Points on Curve
    ctx.fillStyle = '#60a5fa';
    points.forEach(pt => {
        var cx = padding + (pt.x / maxX) * w;
        var cy = (canvas.height - padding) - (pt.y / maxY) * h;
        ctx.beginPath();
        ctx.arc(cx, cy, 4, 0, Math.PI * 2);
        ctx.fill();
    });
}
</script>
</head>
<body>
    <form id="form1" runat="server">
        <!-- Replaced hardcoded sidebar with the reusable Sidebar User Control -->
        <uc:Sidebar ID="Sidebar1" runat="server" />

        <div class="main-content">
            <div class="header-top">
                <div class="header-title">
                    Interactive Virtual Laboratory 
                    <span style="font-size: 13px; background: #e0f2fe; color: #0284c7; padding: 4px 10px; border-radius: 6px;">IGCSE / SPM Aligned</span>
                </div>
            </div>

            <div class="lab-tabs">
                <asp:Button ID="btnTabPhysics" runat="server" Text="Physics Lab" CssClass="tab-btn active-tab" OnClick="SwitchTab_Click" CommandArgument="Physics" />
                <asp:Button ID="btnTabChemistry" runat="server" Text="Chemistry Lab" CssClass="tab-btn" OnClick="SwitchTab_Click" CommandArgument="Chemistry" />
                <asp:Button ID="btnTabBiology" runat="server" Text="Biology Lab" CssClass="tab-btn" OnClick="SwitchTab_Click" CommandArgument="Biology" />
            </div>

            <asp:MultiView ID="mvLabPanels" runat="server" ActiveViewIndex="0">
                
                <!-- TAB 1: PHYSICS LAB -->
                <asp:View ID="vwPhysics" runat="server">
                    <div class="workspace">
                        <div>
                            <div class="card">
                                <div class="sim-canvas">
                                    <canvas id="scienceCanvas" width="560" height="400" style="width: 100%; height: 100%;"></canvas>
                                </div>
                            </div>
                            <div class="card">
                                <strong>AI Curriculum Insight (Physics)</strong>
                                <div style="margin-top: 8px; font-size: 13px; color: var(--text-muted);">
                                    <asp:Literal ID="litPhysicsAi" runat="server" Text="Select an experiment and adjust parameters to evaluate mechanics, electricity, or waves."></asp:Literal>
                                </div>
                            </div>
                        </div>
                        <div>
                            <div class="card">
                                <h3>Physics Controls</h3>
                                <div class="control-group">
                                    <label>Select SPM / IGCSE Experiment</label>
                                    <asp:DropDownList ID="ddlPhysicsExp" runat="server" CssClass="form-control">
                                        <asp:ListItem Text="Projectile Motion (Mechanics)" Value="Projectile" />
                                        <asp:ListItem Text="Ohm's Law & Resistance (Electricity)" Value="OhmsLaw" />
                                        <asp:ListItem Text="Snell's Law & Refraction (Waves)" Value="Refraction" />
                                    </asp:DropDownList>
                                </div>
                                <div class="control-group">
                                    <label>Initial Velocity / Parameter (m/s)</label>
                                    <asp:TextBox ID="txtPhysVelocity" runat="server" CssClass="form-control" Text="15"></asp:TextBox>
                                </div>
                                <div class="control-group">
                                    <label>Launch Angle / Secondary Parameter (°)</label>
                                    <asp:TextBox ID="txtPhysAngle" runat="server" CssClass="form-control" Text="45"></asp:TextBox>
                                </div>
                                <asp:Button ID="btnRunPhysics" runat="server" Text="Run Simulation" CssClass="btn-primary" OnClick="btnRunPhysics_Click" />
                            </div>
                            <div class="card">
                                <h3>Real-Time Data</h3>
                                <div style="font-size: 13px; margin-bottom: 8px;">Primary Metric: <span style="font-weight:bold;"><asp:Label ID="lblPhysHeight" runat="server" Text="12.45 m" /></span></div>
                                <div style="font-size: 13px;">Secondary Metric: <span style="font-weight:bold;"><asp:Label ID="lblPhysDistance" runat="server" Text="24.81 m" /></span></div>
                            </div>
                        </div>
                    </div>
                </asp:View>

                <!-- TAB 2: CHEMISTRY LAB -->
                <asp:View ID="vwChemistry" runat="server">
                    <div class="workspace">
                        <div>
                            <div class="card">
                                <div class="sim-canvas">
                                    <canvas id="CanvasChem" width="560" height="300" style="width: 100%; height: 100%; display:none;"></canvas>
                                    <div id="chemPlaceholder" style="color: #94a3b8; font-size: 14px;">[ Chemistry Reaction Graph Viewport ]</div>
                                </div>
                            </div>
                            <div class="card">
                                <strong>AI Curriculum Insight (Chemistry)</strong>
                                <div style="margin-top: 8px; font-size: 13px; color: var(--text-muted);">
                                    <asp:Literal ID="litChemAi" runat="server" Text="Select chemical substances and amounts to plot reaction rates or titration curves."></asp:Literal>
                                </div>
                            </div>
                        </div>
                        <div>
                            <div class="card">
                                <h3>Chemistry Controls</h3>
                                <div class="control-group">
                                    <label>Select Experiment Type</label>
                                    <asp:DropDownList ID="ddlChemistryExp" runat="server" CssClass="form-control">
                                        <asp:ListItem Text="Acid-Base Titration" Value="Titration" />
                                        <asp:ListItem Text="Rate of Reaction (Metal + Acid)" Value="ReactionRate" />
                                    </asp:DropDownList>
                                </div>
                                <div class="control-group">
                                    <label>Select Chemical Substance A (Reactant)</label>
                                    <asp:DropDownList ID="ddlChemSubstance" runat="server" CssClass="form-control">
                                        <asp:ListItem Text="Hydrochloric Acid (HCl)" Value="HCl" />
                                        <asp:ListItem Text="Sulfuric Acid (H2SO4)" Value="H2SO4" />
                                        <asp:ListItem Text="Sodium Hydroxide (NaOH)" Value="NaOH" />
                                    </asp:DropDownList>
                                </div>
                                <div class="control-group">
                                    <label>Concentration / Volume Amount</label>
                                    <asp:TextBox ID="txtChemAmount" runat="server" CssClass="form-control" Text="25.0 mL (0.1M)"></asp:TextBox>
                                </div>
                                <asp:Button ID="btnRunChemistry" runat="server" Text="Execute Chemical Reaction" CssClass="btn-primary" OnClick="btnRunChemistry_Click" />
                            </div>
                            <div class="card">
                                <h3>Reaction Readout</h3>
                                <div style="font-size: 13px; margin-bottom: 8px;">Endpoint / Rate: <span style="font-weight:bold;"><asp:Label ID="lblChemOutput" runat="server" Text="pH 7.00" /></span></div>
                            </div>
                        </div>
                    </div>
                </asp:View>

                <!-- TAB 3: BIOLOGY LAB -->
                <asp:View ID="vwBiology" runat="server">
                    <div class="workspace">
                        <div>
                            <div class="card">
                                <div class="sim-canvas">
                                    <div id="bioPlaceholder" style="color: #94a3b8; font-size: 14px;">[ Biology Graph Viewport: Enzyme & Osmosis Kinetics ]</div>
                                </div>
                            </div>
                            <div class="card">
                                <strong>AI Curriculum Insight (Biology)</strong>
                                <div style="margin-top: 8px; font-size: 13px; color: var(--text-muted);">
                                    <asp:Literal ID="litBioAi" runat="server" Text="Analyze graph-based biological processes like enzyme activity curves or osmosis rates."></asp:Literal>
                                </div>
                            </div>
                        </div>
                        <div>
                            <div class="card">
                                <h3>Biology Graph Controls</h3>
                                <div class="control-group">
                                    <label>Select Graph-Based Experiment</label>
                                    <asp:DropDownList ID="ddlBiologyExp" runat="server" CssClass="form-control">
                                        <asp:ListItem Text="Enzyme Activity vs Temperature (°C)" Value="EnzymeTemp" />
                                        <asp:ListItem Text="Osmosis Rate vs Sucrose Concentration (%)" Value="Osmosis" />
                                        <asp:ListItem Text="Photosynthesis Rate vs Light Intensity (lux)" Value="Photosynthesis" />
                                    </asp:DropDownList>
                                </div>
                                <div class="control-group">
                                    <label>Independent Variable Value</label>
                                    <asp:TextBox ID="txtBioParam" runat="server" CssClass="form-control" Text="37"></asp:TextBox>
                                </div>
                                <asp:Button ID="btnRunBiology" runat="server" Text="Generate Biological Graph" CssClass="btn-primary" OnClick="btnRunBiology_Click" />
                            </div>
                            <div class="card">
                                <h3>Kinetic Data</h3>
                                <div style="font-size: 13px;">Reaction Efficiency: <span style="font-weight:bold;"><asp:Label ID="lblBioRate" runat="server" Text="100% Optimum" /></span></div>
                            </div>
                        </div>
                    </div>
                </asp:View>

            </asp:MultiView>
        </div>
    </form>
</body>
</html>