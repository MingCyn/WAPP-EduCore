<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="learningProgression.aspx.cs"
    Inherits="WAPP_EduCore.pages.Student.WebForm6" %>

<%@ Register Src="~/pages/Shared/Sidebar.ascx" TagName="Sidebar" TagPrefix="uc" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Lesson</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Merriweather:wght@400;700&display=swap"
        rel="stylesheet" />
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/student.css" rel="stylesheet" type="text/css" />
    <link href="/Content/student-upgrade.css" rel="stylesheet" />
    <script src="/Content/fx.js"></script>
    <style>
        /* ===== NEW: AI quota / rate-limit message inside the note modal ===== */
        .ai-limit {
            text-align: center;
            padding: 18px 16px;
            background: #fff7ed;
            border: 1px solid #fdba74;
            border-radius: 12px;
            color: #9a3412;
        }

        .ai-limit-icon {
            font-size: 32px;
            margin-bottom: 4px;
        }

        .ai-limit h4 {
            margin: 0 0 6px;
            font-size: 16px;
            color: #7c2d12;
        }

        .ai-limit p {
            margin: 0 0 12px;
            font-size: 13.5px;
            line-height: 1.5;
            color: #9a3412;
        }

        .ai-limit-timer {
            display: inline-block;
            background: #ffffff;
            border: 1px solid #fdba74;
            border-radius: 10px;
            padding: 6px 18px;
            font-size: 24px;
            font-weight: 700;
            font-variant-numeric: tabular-nums;
            color: #9a3412;
        }

        .ai-limit-label {
            display: block;
            margin-top: 6px;
            font-size: 11px;
            letter-spacing: 1px;
            text-transform: uppercase;
            color: #c2410c;
        }

        .ai-ok {
            color: #15803d;
        }

        .ai-error-plain {
            color: #b91c1c;
            font-weight: 600;
        }

        #aiSubmitQuery:disabled {
            opacity: 0.5;
            cursor: not-allowed;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <uc:Sidebar ID="Sidebar1" runat="server" />
            <main class="main">
                <div class="topbar">
                    <div class="lv-heading">
                        <span class="title">
                            <asp:Literal ID="litCourseTitle" runat="server" Mode="Encode"></asp:Literal></span>
                        <span class="q-chip" id="chipBox" runat="server">Lesson
                           
                            <asp:Literal ID="litLessonNo" runat="server"></asp:Literal>
                            of
                           
                            <asp:Literal ID="litLessonTotal" runat="server"></asp:Literal></span>
                    </div>
                    <div class="lv-top-right">
                        <span>Course progress: <strong>
                            <asp:Literal ID="litPct" runat="server"></asp:Literal>% Complete</strong></span>
                        <asp:HyperLink ID="lnkBackCourses" runat="server" NavigateUrl="~/pages/Student/myCourses.aspx"
                            CssClass="btn-ghost">Back to Courses</asp:HyperLink>
                    </div>
                </div>

                <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="result-box">
                    <h3>This course has no lessons yet.</h3>
                    <p style="margin-top: 12px">
                        <a href="myCourses.aspx" class="btn-solid">Back to courses</a>
                    </p>
                </asp:Panel>

                <asp:Panel ID="pnlViewer" runat="server">
                    <div class="lv-layout">

                        <!-- left: syllabus -->
                        <div class="lv-side">
                            <h3>Course syllabus</h3>
                            <asp:Repeater ID="rptSyllabus" runat="server">
                                <ItemTemplate>
                                    <a class='<%# ItemCss(Eval("lesson_id"), Eval("done")) %>'
                                        href='<%# LessonHref(Eval("lesson_id")) %>'
                                        title='<%#: Eval("lesson_title") %>'>
                                        <span class="lv-check"></span>
                                        <span class="lv-name">Lesson <%# Container.ItemIndex + 1 %>: <%#: Eval("lesson_title") %></span>
                                    </a>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>

                        <!-- centre: lesson -->
                        <div class="lv-main">
                            <asp:Literal ID="litVideo" runat="server" Mode="PassThrough"></asp:Literal>
                            <h2 class="lv-title">
                                <asp:Literal ID="litLessonTitle" runat="server" Mode="Encode"></asp:Literal>
                            </h2>
                            <p class="lv-desc">
                                <asp:Literal ID="litLessonDesc" runat="server" Mode="Encode"></asp:Literal>
                            </p>

                            <asp:Panel ID="pnlContent" runat="server" CssClass="lv-content">
                                <asp:Literal ID="litContent" runat="server" Mode="PassThrough"></asp:Literal>
                            </asp:Panel>

                            <div class="lv-actions">
                                <asp:HyperLink ID="lnkPrev" runat="server" CssClass="btn-ghost">Previous Lesson</asp:HyperLink>
                                <asp:Button ID="btnComplete" runat="server" Text="Mark as Complete"
                                    CssClass="btn-solid" OnClick="btnComplete_Click" />
                                <span class="lv-done" id="doneBox" runat="server">&#10003; Lesson completed</span>
                                <asp:HyperLink ID="lnkQuiz" runat="server" CssClass="btn-ghost">Take Quiz</asp:HyperLink>
                                <asp:HyperLink ID="lnkNext" runat="server" CssClass="btn-solid">Next Lesson</asp:HyperLink>
                            </div>
                        </div>

                        <!-- right: tutor + progress + NotebookLM widget -->
                        <div class="lv-right">
                            <div class="tutor-card">
                                <div class="tutor-head">
                                    <span class="tutor-avatar">
                                        <asp:Literal ID="litTutorInitial" runat="server" Mode="Encode"></asp:Literal></span>
                                    <div>
                                        <div class="tutor-name">
                                            <asp:Literal ID="litTutorName" runat="server" Mode="Encode"></asp:Literal>
                                        </div>
                                        <div class="tutor-role">
                                            <asp:Literal ID="litTutorRole" runat="server" Mode="Encode"></asp:Literal>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <asp:HyperLink ID="lnkDiscuss" runat="server" CssClass="btn-ghost" Style="display: block;
                                text-align: center; margin-bottom: 22px">Open Lesson Discussion</asp:HyperLink>
                            <h3>Your progress</h3>
                            <div class="lv-bar">
                                <div class="lv-bar-fill" id="barFill" runat="server"></div>
                            </div>
                            <small style="color: #6b7385">
                                <asp:Literal ID="litDoneCount" runat="server"></asp:Literal>
                                of
                               
                                <asp:Literal ID="litTotalCount" runat="server"></asp:Literal>
                                lessons completed</small>

                            <!-- NotebookLM Course Assistant Widget Added Below Progress -->
                            <div style="margin-top: 30px; padding: 16px; background: #f8fafc; border: 1px solid #e2e8f0;
                                border-radius: 8px;">
                                <h4 style="margin-top: 0; color: #1e293b; font-size: 15px; margin-bottom: 8px;">📚 Course
                                    Study Notebook</h4>
                                <p style="font-size: 12px; color: #64748b; line-height: 1.4; margin-bottom: 12px;">
                                    Highlight
                                    any text on the left lesson panel
                                    to create and save a structured study note directly
                                    to
                                    this course notebook.
                                </p>
                                <button type="button" id="sidebarCreateNoteBtn" disabled="disabled" class="btn-solid"
                                    style="width: 100%; text-align: center; background: #cbd5e1; cursor: not-allowed;
                                    border: none; padding: 8px; border-radius: 6px; font-weight: 600; font-size: 13px;
                                    color: white;">
                                    ✨ Create Note from Selection
                               
                                </button>
                                <a href="savedNotes.aspx?course=<%= _courseId %>" style="display: block; margin-top: 10px;
                                    font-size: 13px; color: #4f46e5; font-weight: 600">View my saved notes</a>
                            </div>
                        </div>

                    </div>
                </asp:Panel>
            </main>
        </div>

        <!-- Floating Popup Toolbar -->
        <div id="aiPopupBtn" style="display: none; position: absolute; z-index: 1000; background: #4f46e5;
            color: white; padding: 6px 12px; border-radius: 6px; cursor: pointer; font-size: 13px;
            font-weight: 600; box-shadow: 0 4px 12px rgba(0,0,0,0.15);">
            ✨ Create Note from Highlight
       
        </div>

        <!-- Interactive Modal Dialog -->
        <div id="aiModal" style="display: none; position: fixed; top: 0; left: 0; width: 100%;
            height: 100%; background: rgba(0,0,0,0.5); z-index: 2000; align-items: center;
            justify-content: center;">
            <div style="background: white; width: 500px; max-width: 90%; padding: 24px; border-radius: 12px;
                box-shadow: 0 10px 25px rgba(0,0,0,0.2);">
                <h3 style="margin-top: 0; color: #1e293b;">EduCore AI - Course Study Note</h3>
                <p style="font-size: 12px; color: #64748b; margin-bottom: 4px;">
                    Selected Highlight:
               
                </p>
                <div id="aiSelectedSnippet" style="font-size: 13px; color: #334155; background: #f8fafc;
                    padding: 10px; border-left: 4px solid #4f46e5; max-height: 100px; overflow-y: auto;
                    border-radius: 4px;">
                </div>

                <p style="font-size: 12px; color: #64748b; margin-top: 12px; margin-bottom: 4px;">
                    Add
                    specific instructions or questions (Optional):
                </p>
                <textarea id="aiUserQuery" placeholder="e.g., Explain this term simply with bullet points..."
                    style="width: 100%; height: 60px; padding: 8px; border: 1px solid #cbd5e1; border-radius: 6px;
                    box-sizing: border-box;"></textarea>

                <div style="margin-top: 14px; text-align: right;">
                    <button type="button" id="aiCloseModal" class="btn-ghost" style="margin-right: 8px;">
                        Cancel</button>
                    <button type="button" id="aiSubmitQuery" class="btn-solid" style="background: #4f46e5;
                        color: white;">
                        Save Note to Notebook</button>
                </div>
                <div id="aiResponseArea" style="margin-top: 16px; padding: 12px; background: #f1f5f9;
                    border-radius: 6px; display: none; font-size: 14px; color: #334155; max-height: 260px;
                    overflow-y: auto;">
                </div>
            </div>
        </div>

        <script>
let savedSelectedText = "";
const currentCourseId = "<%= _courseId %>";
            const currentLessonId = "<%= _lessonId %>";

            /* ================= AI quota / rate-limit helpers ================= */

            let aiLimitTimer = null;

            function stopAiLimitTimer() {
                if (aiLimitTimer) {
                    clearInterval(aiLimitTimer);
                    aiLimitTimer = null;
                }
                const submitEl = document.getElementById("aiSubmitQuery");
                if (submitEl) submitEl.disabled = false;
            }

            function escapeHtml(s) {
                return String(s == null ? "" : s)
                    .replace(/&/g, "&amp;")
                    .replace(/</g, "&lt;")
                    .replace(/>/g, "&gt;")
                    .replace(/"/g, "&quot;")
                    .replace(/'/g, "&#39;");
            }

            function isQuotaError(msg) {
                return /429|quota|rate.?limit|RESOURCE_EXHAUSTED/i.test(msg || "");
            }

            // Reads "Please retry in 2h17m18.163694449s" (or "41m3s", "45s") and returns seconds
            function parseRetrySeconds(msg) {
                const m = /retry in\s*(?:(\d+)h)?(?:(\d+)m(?!s))?(?:(\d+(?:\.\d+)?)s)?/i.exec(msg || "");
                if (!m) return null;
                const total = (+m[1] || 0) * 3600 + (+m[2] || 0) * 60 + Math.ceil(+m[3] || 0);
                return total > 0 ? total : null;
            }

            function pad2(n) {
                return n < 10 ? "0" + n : "" + n;
            }

            function formatClock(sec) {
                const h = Math.floor(sec / 3600);
                const m = Math.floor((sec % 3600) / 60);
                const s = sec % 60;
                return (h > 0 ? pad2(h) + ":" : "") + pad2(m) + ":" + pad2(s);
            }

            function formatHuman(sec) {
                const h = Math.floor(sec / 3600);
                const m = Math.floor((sec % 3600) / 60);
                if (h > 0) return h + (h === 1 ? " hour " : " hours ") + m + (m === 1 ? " minute" : " minutes");
                if (m > 0) return m + (m === 1 ? " minute" : " minutes");
                return sec + " seconds";
            }

            function showAiError(box, rawMsg) {
                stopAiLimitTimer();
                box.style.display = "block";

                // Any non-quota error: plain red text, safely escaped
                if (!isQuotaError(rawMsg)) {
                    box.innerHTML = '<div class="ai-error-plain">' +
                        escapeHtml(rawMsg || "Something went wrong. Please try again.") + '</div>';
                    return;
                }

                let remaining = parseRetrySeconds(rawMsg);

                // Quota error but no wait time found
                if (remaining === null) {
                    box.innerHTML =
                        '<div class="ai-limit"><div class="ai-limit-icon">⏳</div>' +
                        '<h4>Daily AI limit reached</h4>' +
                        '<p>The AI note generator has reached its free usage limit for now. ' +
                        'Please try again later today.</p></div>';
                    return;
                }

                box.innerHTML =
                    '<div class="ai-limit">' +
                    '<div class="ai-limit-icon">⏳</div>' +
                    '<h4>You\'ve hit today\'s AI limit</h4>' +
                    '<p>The daily AI quota has been used up. It resets in about <strong>' +
                    formatHuman(remaining) + '</strong>. Your highlight is not lost, so come back then.</p>' +
                    '<div class="ai-limit-timer" id="aiLimitTimer">' + formatClock(remaining) + '</div>' +
                    '<span class="ai-limit-label">until you can save AI notes again</span>' +
                    '</div>';

                // Block further clicks while the limit is active
                const submitEl = document.getElementById("aiSubmitQuery");
                if (submitEl) submitEl.disabled = true;

                const timerEl = document.getElementById("aiLimitTimer");
                aiLimitTimer = setInterval(function () {
                    remaining--;
                    if (remaining <= 0) {
                        stopAiLimitTimer();
                        box.innerHTML =
                            '<div class="ai-limit"><div class="ai-limit-icon">✅</div>' +
                            '<h4>Limit reset</h4>' +
                            '<p>You can save your note again now.</p></div>';
                        return;
                    }
                    if (timerEl) timerEl.textContent = formatClock(remaining);
                }, 1000);
            }

            /* ================= Highlight + modal logic ================= */

            // Listen for text highlights on the page
            document.addEventListener("mouseup", function (e) {
                const selection = window.getSelection();
                if (!selection) return;

                const text = selection.toString().trim();
                const popup = document.getElementById("aiPopupBtn");
                const modal = document.getElementById("aiModal");
                const sidebarBtn = document.getElementById("sidebarCreateNoteBtn");

                if (!popup || !modal || !sidebarBtn) return;
                if (modal.style.display === "flex") return;

                const contentPanel = document.getElementById("<%= pnlContent.ClientID %>");
                if (text.length > 3 && contentPanel && selection.anchorNode && contentPanel.contains(selection.anchorNode)) {
                    savedSelectedText = text;

                    // Enable sidebar button since text is highlighted
                    sidebarBtn.removeAttribute("disabled");
                    sidebarBtn.style.background = "#4f46e5";
                    sidebarBtn.style.cursor = "pointer";

                    // Show floating popup near cursor
                    popup.style.display = "block";
                    popup.style.top = (e.pageY + 10) + "px";
                    popup.style.left = (e.pageX + 10) + "px";
                } else {
                    const targetNode = e.target;
                    if (targetNode instanceof Node && !popup.contains(targetNode) && !sidebarBtn.contains(targetNode)) {
                        popup.style.display = "none";
                    }
                }
            });

function openNoteModal() {
    if (!savedSelectedText) return;
    const snippetEl = document.getElementById("aiSelectedSnippet");
    const modalEl = document.getElementById("aiModal");
    const popupEl = document.getElementById("aiPopupBtn");

    if (snippetEl) snippetEl.innerText = "\"" + savedSelectedText + "\"";
    if (modalEl) modalEl.style.display = "flex";
    if (popupEl) popupEl.style.display = "none";
}

const popupBtnEl = document.getElementById("aiPopupBtn");
if (popupBtnEl) popupBtnEl.addEventListener("click", openNoteModal);

const sidebarCreateBtnEl = document.getElementById("sidebarCreateNoteBtn");
if (sidebarCreateBtnEl) sidebarCreateBtnEl.addEventListener("click", openNoteModal);

const closeModalEl = document.getElementById("aiCloseModal");
if (closeModalEl) {
    closeModalEl.addEventListener("click", function () {
        const modalEl = document.getElementById("aiModal");
        const respAreaEl = document.getElementById("aiResponseArea");
        const userQueryEl = document.getElementById("aiUserQuery");

        stopAiLimitTimer();

        if (modalEl) modalEl.style.display = "none";
        if (respAreaEl) respAreaEl.style.display = "none";
        if (userQueryEl) userQueryEl.value = "";
    });
}

const submitQueryEl = document.getElementById("aiSubmitQuery");
if (submitQueryEl) {
    submitQueryEl.addEventListener("click", function () {
        const userQueryEl = document.getElementById("aiUserQuery");
        const respArea = document.getElementById("aiResponseArea");

        const query = (userQueryEl ? userQueryEl.value : "") || "Summarize and explain this section.";

        stopAiLimitTimer();
        if (respArea) {
            respArea.style.display = "block";
            respArea.textContent = "Generating note and saving to course notebook...";
        }

        fetch('/pages/Student/AskAI.ashx', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                highlightedText: savedSelectedText,
                courseId: currentCourseId,
                lessonId: currentLessonId,
                userQuery: query
            })
        })
            .then(res => res.json())
            .then(data => {
                if (!respArea) return;
                if (data.success) {
                    respArea.innerHTML =
                        "<strong class='ai-ok'>✓ Note successfully saved to course notebook!</strong><br/><br/>" +
                        escapeHtml(data.answer).replace(/\n/g, '<br/>');
                } else {
                    showAiError(respArea, String(data.answer || data.message || ""));
                }
            })
            .catch(err => {
                if (respArea) {
                    respArea.innerHTML = '<div class="ai-error-plain">Error connecting to AI service.</div>';
                }
            });
    });
}
</script>
    </form>
</body>
</html>
