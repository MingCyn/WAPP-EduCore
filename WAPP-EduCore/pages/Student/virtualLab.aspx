<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="virtualLab.aspx.cs" Inherits="WAPPEduCore.virtualLab"
    Async="true" %>

<%@ Register Src="~/pages/Shared/Sidebar.ascx" TagName="Sidebar" TagPrefix="uc" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Merriweather:wght@400;700&display=swap"
        rel="stylesheet" />
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
        }

        html, body { height: 100%; margin: 0; padding: 0; background-color: var(--bg-page); color: var(--ink); }

        #form1 { display: flex; min-height: 100vh; width: 100vw; overflow-x: hidden; }

        .main-content {
            flex: 1; margin-left: 260px; display: flex; flex-direction: column; overflow-y: auto;
            padding: 40px; min-height: 100vh; box-sizing: border-box; background-color: var(--bg-page);
        }

        .header-top { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }
        .header-title { font-size: 24px; font-weight: 600; color: var(--ink); display: flex; align-items: center; gap: 12px; }

        .lab-tabs { display: flex; gap: 10px; margin-bottom: 24px; border-bottom: 2px solid var(--border); padding-bottom: 12px; }
        .tab-btn {
            background: var(--card); border: 1px solid var(--border); padding: 8px 16px; border-radius: 6px;
            font-weight: 600; cursor: pointer; color: var(--muted); transition: all 0.2s; font-size: 13px;
        }
        .tab-btn.active-tab { background-color: var(--accent); color: white; border-color: var(--accent); }

        /* Section labels */
        .section-head { margin: 4px 0 16px; }
        .section-head .section-tag {
            display: inline-block; font-size: 11px; font-weight: 700; letter-spacing: .5px; text-transform: uppercase;
            color: var(--accent); background: #eef2fb; padding: 3px 8px; border-radius: 4px; margin-bottom: 6px;
        }
        .section-head h2 { font-size: 20px; font-weight: 700; color: var(--ink); margin: 0 0 4px; }
        .section-head p { font-size: 13px; color: var(--muted); margin: 0; }
        .section-divider { border: 0; border-top: 2px solid var(--border); margin: 32px 0 24px; }

        .workspace { display: grid; grid-template-columns: 2fr 1fr; gap: 24px; }

        .card {
            background: var(--card); border: 1px solid var(--border); border-radius: 10px; padding: 20px;
            box-shadow: 0 6px 18px rgba(16,24,40,0.04); margin-bottom: 20px;
        }
        .card h3 { font-size: 17px; font-weight: 600; color: var(--ink); margin-bottom: 12px; }

        .sim-canvas {
            background: #0f1b45; border-radius: 8px; height: 420px; display: flex; align-items: center;
            justify-content: center; color: #c3c9de; font-size: 14px; position: relative; overflow: hidden;
        }
        .sim-canvas canvas { width: 100%; height: 100%; display: block; }

        .ai-insight-text { margin-top: 8px; font-size: 13px; color: var(--muted); }

        .control-group { margin-bottom: 16px; }
        .control-group label { display: block; font-size: 13px; color: var(--text); margin-bottom: 6px; font-weight: 500; }
        .form-control { width: 100%; padding: 10px; border: 1px solid var(--border); border-radius: 6px; box-sizing: border-box; background-color: #fff; color: var(--ink); font-size: 14px; }

        .btn-primary {
            background-color: var(--btn); color: white; border: none; padding: 10px 16px; width: 100%;
            border-radius: 6px; font-weight: 600; font-size: 13px; cursor: pointer; transition: background-color 0.2s;
        }
        .btn-primary:hover { background-color: var(--btn-hover); }

        /* Virtual experiments (embedded) */
        .exp-picker { max-width: 560px; margin-bottom: 16px; }
        .exp-layout { display: grid; grid-template-columns: 2fr 1fr; gap: 24px; align-items: start; }
        .exp-stage { background: var(--card); border: 1px solid var(--border); border-radius: 10px; padding: 16px; box-shadow: 0 6px 18px rgba(16,24,40,0.04); }
        .exp-toolbar { display: flex; justify-content: space-between; align-items: center; gap: 12px; margin-bottom: 12px; }
        .exp-toolbar strong { font-size: 15px; color: var(--ink); }
        .btn-fs {
            background: var(--btn); color: #fff; border: 0; border-radius: 6px; padding: 8px 14px;
            font-size: 12px; font-weight: 600; cursor: pointer; white-space: nowrap;
        }
        .btn-fs:hover { background: var(--btn-hover); }
        .exp-frame-host { background: #0f1b45; border-radius: 8px; overflow: hidden; min-height: 640px; }
        .exp-frame-host iframe { display: block; width: 100%; height: 640px; border: 0; }
        .exp-credit { font-size: 12px; color: var(--muted); margin: 10px 0 0; }
        .exp-credit a { color: var(--accent-link); }
        .exp-info { background: #fbead6; border: 1px solid #efdcc3; border-radius: 10px; padding: 20px; }
        .exp-info h4 { font-size: 13px; text-transform: uppercase; letter-spacing: .4px; color: var(--muted); margin: 0 0 6px; }
        .exp-info p { font-size: 14px; line-height: 1.6; color: var(--ink); margin: 0 0 18px; }
        .exp-info ol { margin: 0 0 14px 18px; padding: 0; }
        .exp-info li { font-size: 13px; line-height: 1.6; color: #333f54; margin-bottom: 6px; }
        .exp-note { font-size: 11px; color: var(--muted); margin: 0; }

        @media (max-width: 1100px) {
            .workspace, .exp-layout { grid-template-columns: 1fr; }
        }
    </style>

    <!-- Simulic embed script: loaded once for the whole page -->
    <script src="https://simulic.com/static/js/embed.js" async></script>

    <script type="text/javascript">
    // Tracks which canvases already have a real graph so the empty-state never overwrites it
    window.__graphDrawn = window.__graphDrawn || {};

    function prepCanvas(canvasId) {
        var canvas = document.getElementById(canvasId);
        if (!canvas) return null;
        // Match the drawing buffer to the displayed size (prevents stretched text)
        var w = canvas.clientWidth || 560, h = canvas.clientHeight || 400;
        if (canvas.width !== w) canvas.width = w;
        if (canvas.height !== h) canvas.height = h;
        var ctx = canvas.getContext('2d');
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        return { canvas: canvas, ctx: ctx };
    }

    function drawEmptyGraph(canvasId, message) {
        if (window.__graphDrawn[canvasId]) return;
        var c = prepCanvas(canvasId);
        if (!c) return;
        c.ctx.fillStyle = '#94a3b8';
        c.ctx.font = '14px Segoe UI, Inter, sans-serif';
        c.ctx.textAlign = 'center';
        c.ctx.fillText(message || 'Run the simulation to plot a graph', c.canvas.width / 2, c.canvas.height / 2);
    }

    function drawSimulationGraph(points, xLabel, yLabel, canvasId) {
        var c = prepCanvas(canvasId || 'canvasPhysics');
        if (!c || !points || points.length === 0) return;
        window.__graphDrawn[canvasId || 'canvasPhysics'] = true;
        var canvas = c.canvas, ctx = c.ctx;

        var padding = 55;
        var w = canvas.width - (padding + 30);
        var h = canvas.height - (padding + 30);

        var maxX = Math.max.apply(null, points.map(function (p) { return p.x; })) || 1;
        var rawMaxY = Math.max.apply(null, points.map(function (p) { return p.y; })) || 1;
        var maxY = rawMaxY * 1.15;

        ctx.fillStyle = '#ffffff';
        ctx.font = 'bold 13px Segoe UI, Inter, sans-serif';
        ctx.textAlign = 'center';
        ctx.fillText(yLabel + " vs " + xLabel, canvas.width / 2, 22);

        ctx.strokeStyle = '#475569';
        ctx.lineWidth = 1.5;
        ctx.beginPath();
        ctx.moveTo(padding, canvas.height - padding);
        ctx.lineTo(canvas.width - 20, canvas.height - padding);
        ctx.moveTo(padding, canvas.height - padding);
        ctx.lineTo(padding, 35);
        ctx.stroke();

        ctx.fillStyle = '#94a3b8';
        ctx.font = '10px Segoe UI, Inter, sans-serif';
        ctx.textAlign = 'right';
        var yTicks = 5, i, val;
        for (i = 0; i <= yTicks; i++) {
            val = (maxY / yTicks) * i;
            var yPos = (canvas.height - padding) - (h / yTicks) * i;
            ctx.fillText(val.toFixed(1), padding - 8, yPos + 4);
            ctx.beginPath(); ctx.moveTo(padding - 3, yPos); ctx.lineTo(padding, yPos); ctx.stroke();
        }

        ctx.textAlign = 'center';
        var xTicks = 5;
        for (i = 0; i <= xTicks; i++) {
            val = (maxX / xTicks) * i;
            var xPos = padding + (w / xTicks) * i;
            ctx.fillText(val.toFixed(1), xPos, canvas.height - padding + 15);
            ctx.beginPath(); ctx.moveTo(xPos, canvas.height - padding); ctx.lineTo(xPos, canvas.height - padding + 3); ctx.stroke();
        }

        ctx.fillStyle = '#cbd5e1';
        ctx.font = '12px Segoe UI, Inter, sans-serif';
        ctx.textAlign = 'center';
        ctx.fillText(xLabel || 'X-Axis', padding + (w / 2), canvas.height - 8);
        ctx.save();
        ctx.translate(16, canvas.height / 2);
        ctx.rotate(-Math.PI / 2);
        ctx.fillText(yLabel || 'Y-Axis', 0, 0);
        ctx.restore();

        ctx.strokeStyle = '#3b82f6';
        ctx.lineWidth = 3;
        ctx.beginPath();
        points.forEach(function (pt, index) {
            var cx = padding + (pt.x / maxX) * w;
            var cy = (canvas.height - padding) - (pt.y / maxY) * h;
            if (index === 0) ctx.moveTo(cx, cy); else ctx.lineTo(cx, cy);
        });
        ctx.stroke();

        ctx.fillStyle = '#60a5fa';
        points.forEach(function (pt) {
            var cx = padding + (pt.x / maxX) * w;
            var cy = (canvas.height - padding) - (pt.y / maxY) * h;
            ctx.beginPath(); ctx.arc(cx, cy, 4, 0, Math.PI * 2); ctx.fill();
        });
    }

    // ---------- Virtual experiments (Simulic embeds) ----------
    var LAB_EXPERIMENTS = {
        physics: [
            {
                id: 'beam-balance',
                name: 'Beam Balance and Weight Box (Measuring Mass)',
                src: 'https://simulic.com/e/IySAEhjUT6',
                iframeTitle: 'Beam balance and weight box',
                objective: 'Understand how a beam balance compares an unknown mass with known standard masses, and practise measuring mass accurately.',
                steps: [
                    'Check that the beam is level with empty pans before you start.',
                    'Place the unknown object on one pan.',
                    'Add standard masses from the weight box to the other pan, largest first, until the beam is level.',
                    'Add up the masses used to get the mass of the object.',
                    'Record the result with its unit and repeat to check your reading.'
                ]
            }
        ],
        chemistry: [
            {
                id: 'organic-tests',
                name: 'Organic Functional Group Tests (Identify Four Unknowns)',
                src: 'https://simulic.com/e/h8CUvCPc69',
                iframeTitle: 'Tests for organic functional groups – identify four unknowns',
                objective: 'Use chemical tests to identify the functional groups present in four unknown organic samples.',
                steps: [
                    'Look at the tests and reagents available in the simulation.',
                    'Apply each test to every unknown sample, one at a time.',
                    'Record every observation in a results table.',
                    'Compare your observations with the expected results, for example bromine water decolourising with alkenes.',
                    'Assign a functional group to each unknown and justify it with your evidence.'
                ]
            },
            {
                id: 'cracking-alkenes',
                name: 'Cracking and Alkenes (Equations and Addition Reactions)',
                src: 'https://simulic.com/e/19REmEdATa',
                iframeTitle: 'Cracking and Alkenes – Virtual Lab, Equations and Addition Reactions',
                objective: 'Explain how cracking breaks long-chain alkanes into shorter alkanes and alkenes, write balanced equations, and describe the addition reactions of alkenes.',
                steps: [
                    'Set up the cracking apparatus and heat the catalyst.',
                    'Collect the gas produced and test it with bromine water.',
                    'Write a balanced equation for the cracking reaction, for example C10H22 → C8H18 + C2H4.',
                    'Explore the addition reactions of the alkene produced.',
                    'Summarise how the products differ from the starting alkane.'
                ]
            }
        ],
        biology: [
            {
                id: 'food-tests',
                name: 'Food Tests (Sugars, Starch, Protein and Fats)',
                src: 'https://simulic.com/e/kw6Muiv868',
                iframeTitle: 'Food tests – sugars, starch, protein and fats in unknown foods',
                objective: 'Carry out the standard food tests to find out which nutrients are present in unknown foods.',
                steps: [
                    'Reducing sugars: Benedict\u2019s solution, heated. Blue changes to green, yellow, orange or brick red.',
                    'Starch: iodine solution. Orange-brown changes to blue-black.',
                    'Protein: biuret test. Blue changes to purple or lilac.',
                    'Fats: ethanol emulsion test. A cloudy white emulsion shows fat is present.',
                    'Record your results for each food and state which nutrients it contains.'
                ]
            },
            {
                id: 'protein-3d',
                name: 'Protein Structure in 3D (Amino Acids to Quaternary Structure)',
                src: 'https://simulic.com/e/3o3wz6gTrz',
                iframeTitle: 'Protein Structure in 3D – from Amino Acids to Quaternary Structure',
                objective: 'Describe primary, secondary, tertiary and quaternary protein structure and the bonds that hold each level together.',
                steps: [
                    'Start with individual amino acids and observe how they join by peptide bonds (primary structure).',
                    'Look for alpha helices and beta sheets held by hydrogen bonds (secondary structure).',
                    'Rotate the 3D model to see how the chain folds (tertiary structure).',
                    'Note the bonds involved: hydrogen, ionic, disulfide and hydrophobic interactions.',
                    'Find how several chains combine in quaternary structure.'
                ]
            }
        ]
    };

    function mountExperimentLab(subject) {
        var select = document.getElementById('selExp_' + subject);
        var host = document.getElementById('frameHost_' + subject);
        if (!select || !host) return; // this tab's view is not rendered right now

        var list = LAB_EXPERIMENTS[subject] || [];
        if (select.options.length === 0) {
            list.forEach(function (e, idx) {
                var opt = document.createElement('option');
                opt.value = idx; opt.textContent = e.name;
                select.appendChild(opt);
            });
        }

        function show(idx) {
            var e = list[idx];
            if (!e) return;
            host.innerHTML = '';
            var f = document.createElement('iframe');
            f.src = e.src;
            f.title = e.iframeTitle;
            f.setAttribute('allow', 'fullscreen');
            f.setAttribute('allowfullscreen', '');
            f.style.border = '0';
            host.appendChild(f);

            document.getElementById('expTitle_' + subject).textContent = e.name;
            document.getElementById('expObjective_' + subject).textContent = e.objective;
            var ol = document.getElementById('expSteps_' + subject);
            ol.innerHTML = '';
            e.steps.forEach(function (s) {
                var li = document.createElement('li'); li.textContent = s; ol.appendChild(li);
            });
            try { sessionStorage.setItem('labExp_' + subject, String(idx)); } catch (ex) { }
        }

        var saved = 0;
        try { saved = parseInt(sessionStorage.getItem('labExp_' + subject), 10) || 0; } catch (ex) { }
        if (saved >= list.length) saved = 0;
        select.value = saved;
        select.addEventListener('change', function () { show(parseInt(select.value, 10)); });
        show(saved);
    }

    function openExperimentFullscreen(subject) {
        var host = document.getElementById('frameHost_' + subject);
        var f = host ? host.querySelector('iframe') : null;
        if (!f) return;
        var req = f.requestFullscreen || f.webkitRequestFullscreen || f.msRequestFullscreen;
        if (req) { req.call(f); } else { window.open(f.src, '_blank'); }
    }
    </script>
</head>
<body>
    <form id="form1" runat="server">
        <uc:Sidebar ID="Sidebar1" runat="server" />

        <div class="main-content">
            <div class="header-top">
                <div class="header-title">
                    Interactive Virtual Laboratory
                   
                    <span style="font-size: 13px; background: #e0f2fe; color: #0284c7; padding: 4px 10px;
                        border-radius: 6px;">IGCSE / SPM Aligned</span>
                </div>
            </div>

            <div class="lab-tabs">
                <asp:Button ID="btnTabPhysics" runat="server" Text="Physics Lab" CssClass="tab-btn active-tab"
                    OnClick="SwitchTab_Click" CommandArgument="Physics" />
                <asp:Button ID="btnTabChemistry" runat="server" Text="Chemistry Lab" CssClass="tab-btn"
                    OnClick="SwitchTab_Click" CommandArgument="Chemistry" />
                <asp:Button ID="btnTabBiology" runat="server" Text="Biology Lab" CssClass="tab-btn"
                    OnClick="SwitchTab_Click" CommandArgument="Biology" />
            </div>

            <asp:MultiView ID="mvLabPanels" runat="server" ActiveViewIndex="0">

                <!-- TAB 1: PHYSICS LAB -->
                <asp:View ID="vwPhysics" runat="server">
                    <!-- SECTION A -->
                    <div class="section-head">
                        <span class="section-tag">Section A</span>
                        <h2>Physics Graph Simulator</h2>
                        <p>Mechanics, electricity and waves: change the parameters and plot the result.</p>
                    </div>
                    <div class="workspace">
                        <div>
                            <div class="card">
                                <div class="sim-canvas">
                                    <canvas id="canvasPhysics" width="560" height="400"></canvas>
                                </div>
                            </div>
                            <div class="card">
                                <strong>AI Curriculum Insight (Physics)</strong>
                                <div class="ai-insight-text">
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
                                <asp:Button ID="btnRunPhysics" runat="server" Text="Run Simulation" CssClass="btn-primary"
                                    OnClick="btnRunPhysics_Click" />
                            </div>
                            <div class="card">
                                <h3>Real-Time Data</h3>
                                <div style="font-size: 13px; margin-bottom: 8px;">Primary Metric: <span style="font-weight: bold;">
                                    <asp:Label ID="lblPhysHeight" runat="server" Text="12.45 m" /></span></div>
                                <div style="font-size: 13px;">Secondary Metric: <span style="font-weight: bold;">
                                    <asp:Label ID="lblPhysDistance" runat="server" Text="24.81 m" /></span></div>
                            </div>
                        </div>
                    </div>

                    <hr class="section-divider" />

                    <!-- SECTION B -->
                    <div class="section-head">
                        <span class="section-tag">Section B</span>
                        <h2>Physics Virtual Experiments</h2>
                        <p>Hands-on practical simulations. Pick an experiment from the list.</p>
                    </div>
                    <div class="exp-picker control-group">
                        <label for="selExp_physics">Select Virtual Experiment</label>
                        <select id="selExp_physics" class="form-control"></select>
                    </div>
                    <div class="exp-layout">
                        <div class="exp-stage">
                            <div class="exp-toolbar">
                                <strong id="expTitle_physics"></strong>
                                <button type="button" class="btn-fs" onclick="openExperimentFullscreen('physics')">⛶
                                    Fullscreen</button>
                            </div>
                            <div id="frameHost_physics" class="exp-frame-host"></div>
                            <p class="exp-credit">Simulation by <a href="https://simulic.com" target="_blank"
                                rel="noopener">Simulic</a></p>
                        </div>
                        <aside class="exp-info">
                            <h4>Learning Objective</h4>
                            <p id="expObjective_physics"></p>
                            <h4>Instructions</h4>
                            <ol id="expSteps_physics"></ol>
                            <p class="exp-note">Tip: use the Fullscreen button for a larger workspace.</p>
                        </aside>
                    </div>
                </asp:View>

                <!-- TAB 2: CHEMISTRY LAB -->
                <asp:View ID="vwChemistry" runat="server">
                    <!-- SECTION A -->
                    <div class="section-head">
                        <span class="section-tag">Section A</span>
                        <h2>Chemistry Reaction &amp; Titration Simulator</h2>
                        <p>Plot titration curves and reaction rates from your chosen reactants.</p>
                    </div>
                    <div class="workspace">
                        <div>
                            <div class="card">
                                <div class="sim-canvas">
                                    <canvas id="canvasChemistry" width="560" height="400"></canvas>
                                </div>
                            </div>
                            <div class="card">
                                <strong>AI Curriculum Insight (Chemistry)</strong>
                                <div class="ai-insight-text">
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
                                <asp:Button ID="btnRunChemistry" runat="server" Text="Execute Chemical Reaction"
                                    CssClass="btn-primary" OnClick="btnRunChemistry_Click" />
                            </div>
                            <div class="card">
                                <h3>Reaction Readout</h3>
                                <div style="font-size: 13px; margin-bottom: 8px;">Endpoint / Rate: <span style="font-weight: bold;">
                                    <asp:Label ID="lblChemOutput" runat="server" Text="pH 7.00" /></span></div>
                            </div>
                        </div>
                    </div>

                    <hr class="section-divider" />

                    <!-- SECTION B -->
                    <div class="section-head">
                        <span class="section-tag">Section B</span>
                        <h2>Chemistry Virtual Experiments</h2>
                        <p>Organic chemistry practicals. Pick an experiment from the list.</p>
                    </div>
                    <div class="exp-picker control-group">
                        <label for="selExp_chemistry">Select Virtual Experiment</label>
                        <select id="selExp_chemistry" class="form-control"></select>
                    </div>
                    <div class="exp-layout">
                        <div class="exp-stage">
                            <div class="exp-toolbar">
                                <strong id="expTitle_chemistry"></strong>
                                <button type="button" class="btn-fs" onclick="openExperimentFullscreen('chemistry')">
                                    ⛶ Fullscreen</button>
                            </div>
                            <div id="frameHost_chemistry" class="exp-frame-host"></div>
                            <p class="exp-credit">Simulation by <a href="https://simulic.com" target="_blank"
                                rel="noopener">Simulic</a></p>
                        </div>
                        <aside class="exp-info">
                            <h4>Learning Objective</h4>
                            <p id="expObjective_chemistry"></p>
                            <h4>Instructions</h4>
                            <ol id="expSteps_chemistry"></ol>
                            <p class="exp-note">Tip: use the Fullscreen button for a larger workspace.</p>
                        </aside>
                    </div>
                </asp:View>

                <!-- TAB 3: BIOLOGY LAB -->
                <asp:View ID="vwBiology" runat="server">
                    <!-- SECTION A -->
                    <div class="section-head">
                        <span class="section-tag">Section A</span>
                        <h2>Biology Kinetics Graph Simulator</h2>
                        <p>Enzyme activity, osmosis and photosynthesis rate curves.</p>
                    </div>
                    <div class="workspace">
                        <div>
                            <div class="card">
                                <div class="sim-canvas">
                                    <canvas id="canvasBiology" width="560" height="400"></canvas>
                                </div>
                            </div>
                            <div class="card">
                                <strong>AI Curriculum Insight (Biology)</strong>
                                <div class="ai-insight-text">
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
                                <asp:Button ID="btnRunBiology" runat="server" Text="Generate Biological Graph" CssClass="btn-primary"
                                    OnClick="btnRunBiology_Click" />
                            </div>
                            <div class="card">
                                <h3>Kinetic Data</h3>
                                <div style="font-size: 13px;">Reaction Efficiency: <span style="font-weight: bold;">
                                    <asp:Label ID="lblBioRate" runat="server" Text="100% Optimum" /></span></div>
                            </div>
                        </div>
                    </div>

                    <hr class="section-divider" />

                    <!-- SECTION B -->
                    <div class="section-head">
                        <span class="section-tag">Section B</span>
                        <h2>Biology Virtual Experiments</h2>
                        <p>Food tests and protein structure. Pick an experiment from the list.</p>
                    </div>
                    <div class="exp-picker control-group">
                        <label for="selExp_biology">Select Virtual Experiment</label>
                        <select id="selExp_biology" class="form-control"></select>
                    </div>
                    <div class="exp-layout">
                        <div class="exp-stage">
                            <div class="exp-toolbar">
                                <strong id="expTitle_biology"></strong>
                                <button type="button" class="btn-fs" onclick="openExperimentFullscreen('biology')">⛶
                                    Fullscreen</button>
                            </div>
                            <div id="frameHost_biology" class="exp-frame-host"></div>
                            <p class="exp-credit">Simulation by <a href="https://simulic.com" target="_blank"
                                rel="noopener">Simulic</a></p>
                        </div>
                        <aside class="exp-info">
                            <h4>Learning Objective</h4>
                            <p id="expObjective_biology"></p>
                            <h4>Instructions</h4>
                            <ol id="expSteps_biology"></ol>
                            <p class="exp-note">Tip: use the Fullscreen button for a larger workspace.</p>
                        </aside>
                    </div>
                </asp:View>

            </asp:MultiView>
        </div>
    </form>

    <script type="text/javascript">
// Runs after the markup exists. Only the active tab's elements are rendered by the MultiView.
(function () {
    ['physics', 'chemistry', 'biology'].forEach(mountExperimentLab);
    drawEmptyGraph('canvasPhysics', 'Run the simulation to plot a graph');
    drawEmptyGraph('canvasChemistry', 'Execute a reaction to plot a graph');
    drawEmptyGraph('canvasBiology', 'Generate a graph to see the curve');
})();
</script>
</body>
</html>
