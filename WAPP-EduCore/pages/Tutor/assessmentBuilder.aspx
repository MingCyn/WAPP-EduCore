<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="assessmentBuilder.aspx.cs" Inherits="WAPP_EduCore.pages.Tutor.assessmentBuilder" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>EduCore - Content Builder</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Source+Serif+4:wght@600;700&display=swap" rel="stylesheet" />
    <link href="../../Content/dashboard.css" rel="stylesheet" type="text/css" />
    <%-- courseBuilder.css supplies the shared nav tweaks, page header and .btn styles --%>
    <link href="../../Content/courseBuilder.css?v=4" rel="stylesheet" type="text/css" />
    <link href="../../Content/assessmentBuilder.css?v=1" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
        <div class="dashboard-layout">

            <%-- Shared nav: use the same include you use on the other tutor pages --%>
            <% Server.Execute("tutorNav.aspx"); %>

            <main class="dashboard-container">

                <!-- Page header -->
                <div class="page-header">
                    <div>
                        <h1 class="page-title">Content Builder</h1>
                        <p class="page-subtitle">Create and configure learning content.</p>
                    </div>
                    <div class="page-actions">
                        <a id="LnkBackToCourse" runat="server" class="btn btn-outline">&larr; Back to Course Builder</a>
                    </div>
                </div>

                <asp:Label ID="LblStatus" runat="server" CssClass="ab-status" Visible="false" />

                <!-- Tabs -->
                <div class="ab-tabs" role="tablist">
                    <button type="button" class="ab-tab active" data-tab="assessment" role="tab">Assessment Builder</button>
                    <button type="button" class="ab-tab" data-tab="interactive" role="tab">Interactive Object</button>
                    <button type="button" class="ab-tab" data-tab="lab" role="tab">Virtual Lab</button>
                </div>

                <!-- ================= TAB 1: ASSESSMENT BUILDER ================= -->
                <section class="ab-panel active" id="panel-assessment" role="tabpanel">
                    <div class="ab-grid">

                        <!-- Left column -->
                        <div class="ab-main">

                            <div class="ab-card">
                                <h3 class="ab-card-title">Assessment Configuration</h3>

                                <div class="ab-field">
                                    <label class="ab-label" for="assessTitle">Assessment Title</label>
                                    <input type="text" id="assessTitle" name="assessTitle" class="ab-input" value="Module 2 Checkpoint Quiz" />
                                </div>

                                <div class="ab-row-3">
                                    <div class="ab-field">
                                        <label class="ab-label" for="assessType">Assessment Type</label>
                                        <select id="assessType" name="assessType" class="ab-input ab-select">
                                            <option selected="selected">Multiple Choice (MCQ)</option>
                                            <option>True / False</option>
                                            <option>Short Answer</option>
                                        </select>
                                    </div>
                                    <div class="ab-field">
                                        <label class="ab-label" for="assessTime">Time Limit</label>
                                        <input type="text" id="assessTime" name="assessTime" class="ab-input" value="20 minutes" />
                                    </div>
                                    <div class="ab-field">
                                        <label class="ab-label" for="assessPass">Passing Score (%)</label>
                                        <input type="text" id="assessPass" name="assessPass" class="ab-input" value="70%" />
                                    </div>
                                </div>

                                <div class="ab-toggle-row">
                                    <div>
                                        <div class="ab-label ab-label-tight">Auto-grade Assessments</div>
                                        <div class="ab-help">Instantly evaluate scores and generate report summaries upon completion.</div>
                                    </div>
                                    <label class="ab-switch"><input type="checkbox" name="autoGrade" checked="checked" /><span class="ab-slider"></span></label>
                                </div>
                            </div>

                            <h2 class="ab-section-title">Question Builder</h2>

                            <div id="questionList">

                                <!-- Question 1 -->
                                <div class="ab-card ab-question" data-uid="1">
                                    <input type="hidden" name="qid" value="1" />
                                    <div class="ab-q-head">
                                        <div class="ab-q-title">
                                            <span class="ab-q-num">Question 1</span>
                                            <select name="difficulty" class="ab-diff">
                                                <option>Easy</option>
                                                <option selected="selected">Medium</option>
                                                <option>Hard</option>
                                            </select>
                                        </div>
                                        <div class="ab-q-points">
                                            <button type="button" class="ab-remove" title="Remove question" hidden="hidden">Remove</button>
                                            <span>Points:</span>
                                            <input type="text" name="points" class="ab-points" value="10" />
                                        </div>
                                    </div>

                                    <div class="ab-field">
                                        <label class="ab-label">Question Prompt</label>
                                        <input type="text" name="prompt" class="ab-input" value="What is the worst-case time complexity of searching for an element in an unsorted array of size N?" />
                                    </div>

                                    <div class="ab-field">
                                        <label class="ab-label">Answer Options</label>
                                        <div class="ab-options">
                                            <label class="ab-option">
                                                <input type="radio" name="correct_1" value="A" />
                                                <span class="ab-radio"></span>
                                                <span class="ab-letter">A.</span>
                                                <input type="text" name="optA" class="ab-option-input" value="O(1)" />
                                                <span class="ab-correct-badge">CORRECT ANSWER</span>
                                            </label>
                                            <label class="ab-option correct">
                                                <input type="radio" name="correct_1" value="B" checked="checked" />
                                                <span class="ab-radio"></span>
                                                <span class="ab-letter">B.</span>
                                                <input type="text" name="optB" class="ab-option-input" value="O(N)" />
                                                <span class="ab-correct-badge">CORRECT ANSWER</span>
                                            </label>
                                            <label class="ab-option">
                                                <input type="radio" name="correct_1" value="C" />
                                                <span class="ab-radio"></span>
                                                <span class="ab-letter">C.</span>
                                                <input type="text" name="optC" class="ab-option-input" value="O(log N)" />
                                                <span class="ab-correct-badge">CORRECT ANSWER</span>
                                            </label>
                                            <label class="ab-option">
                                                <input type="radio" name="correct_1" value="D" />
                                                <span class="ab-radio"></span>
                                                <span class="ab-letter">D.</span>
                                                <input type="text" name="optD" class="ab-option-input" value="O(N log N)" />
                                                <span class="ab-correct-badge">CORRECT ANSWER</span>
                                            </label>
                                        </div>
                                    </div>

                                    <div class="ab-field">
                                        <label class="ab-label">Explanation for Wrong Answers</label>
                                        <input type="text" name="explanation" class="ab-input" value="In an unsorted array, we may have to traverse the entire array to locate the element, resulting in linear search complexity of O(N)." />
                                    </div>
                                </div>

                            </div>

                            <button type="button" class="ab-add-question" id="btnAddQuestion">+&nbsp; Add New Question</button>
                        </div>

                        <!-- Right column -->
                        <aside class="ab-side">

                            <div class="ab-preview">
                                <div class="ab-preview-head">
                                    <span>STUDENT PREVIEW</span>
                                    <span class="ab-live">LIVE</span>
                                </div>
                                <div class="ab-preview-card">
                                    <p class="ab-preview-q" id="pvPrompt"></p>
                                    <div id="pvOptions"></div>
                                </div>
                            </div>

                            <div class="ab-card ab-publish-card">
                                <h3 class="ab-card-title">Publishing Settings</h3>

                                <div class="ab-switch-row">
                                    <span>Randomize Question Order</span>
                                    <label class="ab-switch"><input type="checkbox" name="randomize" /><span class="ab-slider"></span></label>
                                </div>
                                <div class="ab-switch-row">
                                    <span>Show Explanations</span>
                                    <label class="ab-switch"><input type="checkbox" name="showExplanations" checked="checked" /><span class="ab-slider"></span></label>
                                </div>
                                <div class="ab-switch-row">
                                    <span>Allow Retries</span>
                                    <label class="ab-switch"><input type="checkbox" name="allowRetries" checked="checked" /><span class="ab-slider"></span></label>
                                </div>

                                <div class="ab-field ab-field-top">
                                    <label class="ab-label" for="maxAttempts">Max Attempts Allowed</label>
                                    <input type="text" id="maxAttempts" name="maxAttempts" class="ab-input" value="3" />
                                </div>
                            </div>

                            <asp:Button ID="BtnPublishAssessment" runat="server" Text="Publish Assessment" CssClass="ab-btn ab-btn-primary" OnClick="BtnPublishAssessment_Click" />
                            <asp:Button ID="BtnSaveAssessmentDraft" runat="server" Text="Save as Draft" CssClass="ab-btn ab-btn-outline" OnClick="BtnSaveAssessmentDraft_Click" />
                        </aside>

                    </div>
                </section>

                <!-- ================= TAB 2: INTERACTIVE OBJECT ================= -->
                <section class="ab-panel" id="panel-interactive" role="tabpanel">
                    <div class="ab-grid">
                        <div class="ab-main">
                            <div class="ab-card">
                                <h3 class="ab-card-title">Interactive Object Configuration</h3>

                                <div class="ab-field">
                                    <label class="ab-label" for="ioTitle">Object Title</label>
                                    <input type="text" id="ioTitle" name="ioTitle" class="ab-input" value="Interactive Code Playground: Pointer Manipulation" />
                                </div>

                                <div class="ab-row-2">
                                    <div class="ab-field">
                                        <label class="ab-label" for="ioLang">Language</label>
                                        <select id="ioLang" name="ioLang" class="ab-input ab-select">
                                            <option selected="selected">C++</option>
                                            <option>Java</option>
                                            <option>Python</option>
                                            <option>C#</option>
                                        </select>
                                    </div>
                                    <div class="ab-field">
                                        <label class="ab-label" for="ioTimeout">Execution Time Limit (seconds)</label>
                                        <input type="text" id="ioTimeout" name="ioTimeout" class="ab-input" value="5" />
                                    </div>
                                </div>

                                <div class="ab-field">
                                    <label class="ab-label" for="ioStarter">Starter Code</label>
                                    <textarea id="ioStarter" name="ioStarter" class="ab-input ab-textarea ab-code" rows="9"></textarea>
                                    <div class="ab-help">Students see this code when they open the playground.</div>
                                </div>

                                <div class="ab-toggle-row">
                                    <div>
                                        <div class="ab-label ab-label-tight">Allow students to reset code</div>
                                        <div class="ab-help">Adds a reset button that restores the starter code.</div>
                                    </div>
                                    <label class="ab-switch"><input type="checkbox" name="ioAllowReset" checked="checked" /><span class="ab-slider"></span></label>
                                </div>
                            </div>
                        </div>

                        <aside class="ab-side">
                            <div class="ab-card ab-publish-card">
                                <h3 class="ab-card-title">Publishing Settings</h3>
                                <div class="ab-switch-row">
                                    <span>Published (visible to students)</span>
                                    <label class="ab-switch"><input type="checkbox" name="ioPublished" checked="checked" /><span class="ab-slider"></span></label>
                                </div>
                            </div>
                            <asp:Button ID="BtnSaveInteractive" runat="server" Text="Save Configuration" CssClass="ab-btn ab-btn-primary" OnClick="BtnSaveInteractive_Click" />
                            <asp:Button ID="BtnSaveInteractiveDraft" runat="server" Text="Save as Draft" CssClass="ab-btn ab-btn-outline" OnClick="BtnSaveInteractive_Click" />
                        </aside>
                    </div>
                </section>

                <!-- ================= TAB 3: VIRTUAL LAB ================= -->
                <section class="ab-panel" id="panel-lab" role="tabpanel">
                    <div class="ab-grid">
                        <div class="ab-main">
                            <div class="ab-card">
                                <h3 class="ab-card-title">Virtual Lab Configuration</h3>

                                <div class="ab-field">
                                    <label class="ab-label" for="labTitle">Lab Title</label>
                                    <input type="text" id="labTitle" name="labTitle" class="ab-input" value="Lab: Singly Linked List Reversal" />
                                </div>

                                <div class="ab-row-3">
                                    <div class="ab-field">
                                        <label class="ab-label" for="labEnv">Environment</label>
                                        <select id="labEnv" name="labEnv" class="ab-input ab-select">
                                            <option selected="selected">Linux Sandbox</option>
                                            <option>Windows Sandbox</option>
                                            <option>Container (Docker)</option>
                                        </select>
                                    </div>
                                    <div class="ab-field">
                                        <label class="ab-label" for="labMemory">Memory</label>
                                        <select id="labMemory" name="labMemory" class="ab-input ab-select">
                                            <option>512 MB</option>
                                            <option selected="selected">1 GB</option>
                                            <option>2 GB</option>
                                        </select>
                                    </div>
                                    <div class="ab-field">
                                        <label class="ab-label" for="labSession">Session Limit (mins)</label>
                                        <input type="text" id="labSession" name="labSession" class="ab-input" value="60" />
                                    </div>
                                </div>

                                <div class="ab-field">
                                    <label class="ab-label" for="labInstructions">Lab Instructions</label>
                                    <textarea id="labInstructions" name="labInstructions" class="ab-input ab-textarea" rows="7"></textarea>
                                </div>
                            </div>
                        </div>

                        <aside class="ab-side">
                            <div class="ab-card ab-publish-card">
                                <h3 class="ab-card-title">Publishing Settings</h3>
                                <div class="ab-switch-row">
                                    <span>Published (visible to students)</span>
                                    <label class="ab-switch"><input type="checkbox" name="labPublished" /><span class="ab-slider"></span></label>
                                </div>
                            </div>
                            <asp:Button ID="BtnSaveLab" runat="server" Text="Create Environment" CssClass="ab-btn ab-btn-primary" OnClick="BtnSaveLab_Click" />
                            <asp:Button ID="BtnSaveLabDraft" runat="server" Text="Save as Draft" CssClass="ab-btn ab-btn-outline" OnClick="BtnSaveLab_Click" />
                        </aside>
                    </div>
                </section>

            </main>
        </div>

        <!-- Template used by "Add New Question" -->
        <template id="questionTemplate">
            <div class="ab-card ab-question" data-uid="__UID__">
                <input type="hidden" name="qid" value="__UID__" />
                <div class="ab-q-head">
                    <div class="ab-q-title">
                        <span class="ab-q-num">Question</span>
                        <select name="difficulty" class="ab-diff">
                            <option>Easy</option>
                            <option selected="selected">Medium</option>
                            <option>Hard</option>
                        </select>
                    </div>
                    <div class="ab-q-points">
                        <button type="button" class="ab-remove" title="Remove question">Remove</button>
                        <span>Points:</span>
                        <input type="text" name="points" class="ab-points" value="10" />
                    </div>
                </div>
                <div class="ab-field">
                    <label class="ab-label">Question Prompt</label>
                    <input type="text" name="prompt" class="ab-input" placeholder="Type the question here" />
                </div>
                <div class="ab-field">
                    <label class="ab-label">Answer Options</label>
                    <div class="ab-options">
                        <label class="ab-option correct">
                            <input type="radio" name="correct___UID__" value="A" checked="checked" />
                            <span class="ab-radio"></span><span class="ab-letter">A.</span>
                            <input type="text" name="optA" class="ab-option-input" placeholder="Option A" />
                            <span class="ab-correct-badge">CORRECT ANSWER</span>
                        </label>
                        <label class="ab-option">
                            <input type="radio" name="correct___UID__" value="B" />
                            <span class="ab-radio"></span><span class="ab-letter">B.</span>
                            <input type="text" name="optB" class="ab-option-input" placeholder="Option B" />
                            <span class="ab-correct-badge">CORRECT ANSWER</span>
                        </label>
                        <label class="ab-option">
                            <input type="radio" name="correct___UID__" value="C" />
                            <span class="ab-radio"></span><span class="ab-letter">C.</span>
                            <input type="text" name="optC" class="ab-option-input" placeholder="Option C" />
                            <span class="ab-correct-badge">CORRECT ANSWER</span>
                        </label>
                        <label class="ab-option">
                            <input type="radio" name="correct___UID__" value="D" />
                            <span class="ab-radio"></span><span class="ab-letter">D.</span>
                            <input type="text" name="optD" class="ab-option-input" placeholder="Option D" />
                            <span class="ab-correct-badge">CORRECT ANSWER</span>
                        </label>
                    </div>
                </div>
                <div class="ab-field">
                    <label class="ab-label">Explanation for Wrong Answers</label>
                    <input type="text" name="explanation" class="ab-input" placeholder="Why the other options are incorrect" />
                </div>
            </div>
        </template>
    </form>

    <script>
        // @ts-nocheck
        (function () {
            // ---------- Tabs ----------
            var tabs = document.querySelectorAll('.ab-tab');
            var panels = { assessment: 'panel-assessment', interactive: 'panel-interactive', lab: 'panel-lab' };

            function showTab(name) {
                if (!panels[name]) name = 'assessment';
                tabs.forEach(function (t) { t.classList.toggle('active', t.getAttribute('data-tab') === name); });
                Object.keys(panels).forEach(function (k) {
                    document.getElementById(panels[k]).classList.toggle('active', k === name);
                });
            }
            tabs.forEach(function (t) {
                t.addEventListener('click', function () { showTab(t.getAttribute('data-tab')); });
            });
            // courseBuilder links here with ?tab=assessment | interactive | lab
            showTab(new URLSearchParams(window.location.search).get('tab') || 'assessment');

            // ---------- Questions ----------
            var list = document.getElementById('questionList');
            var uid = 1;

            function renumber() {
                var cards = list.querySelectorAll('.ab-question');
                cards.forEach(function (c, i) {
                    c.querySelector('.ab-q-num').textContent = 'Question ' + (i + 1);
                    c.querySelector('.ab-remove').hidden = cards.length === 1;
                });
            }

            function updatePreview() {
                var first = list.querySelector('.ab-question');
                var promptEl = document.getElementById('pvPrompt');
                var optsEl = document.getElementById('pvOptions');
                optsEl.innerHTML = '';
                if (!first) { promptEl.textContent = ''; return; }
                var prompt = first.querySelector('input[name="prompt"]').value.trim();
                promptEl.textContent = prompt || 'Your question will appear here';
                first.querySelectorAll('.ab-option-input').forEach(function (inp) {
                    var text = inp.value.trim();
                    if (!text) return;               // empty option: skip, no gap
                    var row = document.createElement('div');
                    row.className = 'ab-preview-option';
                    row.textContent = text;
                    optsEl.appendChild(row);
                });
            }

            document.getElementById('btnAddQuestion').addEventListener('click', function () {
                uid++;
                var html = document.getElementById('questionTemplate').innerHTML.replace(/__UID__/g, uid);
                list.insertAdjacentHTML('beforeend', html);
                renumber();
                var added = list.lastElementChild.querySelector('input[name="prompt"]');
                if (added) added.focus();
            });

            list.addEventListener('click', function (e) {
                if (e.target.classList.contains('ab-remove')) {
                    e.target.closest('.ab-question').remove();
                    renumber();
                    updatePreview();
                }
            });

            list.addEventListener('change', function (e) {
                if (e.target.type === 'radio') {
                    var opts = e.target.closest('.ab-options').querySelectorAll('.ab-option');
                    opts.forEach(function (o) { o.classList.remove('correct'); });
                    e.target.closest('.ab-option').classList.add('correct');
                }
            });

            list.addEventListener('input', updatePreview);

            renumber();
            updatePreview();
        })();
    </script>

    <%-- Highlight "Assessments" in the shared nav --%>
    <script>
var navLink = document.querySelector('.nav-link[href="assessments.aspx"]');
if (navLink) navLink.classList.add('active');
</script>
</body>
</html>
