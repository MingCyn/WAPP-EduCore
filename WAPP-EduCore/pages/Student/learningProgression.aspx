<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="learningProgression.aspx.cs" Inherits="WAPP_EduCore.pages.Student.WebForm6" %>
<%@ Register Src="~/pages/Shared/Sidebar.ascx" TagName="Sidebar" TagPrefix="uc" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Lesson</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Merriweather:wght@400;700&display=swap" rel="stylesheet" />
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/student.css" rel="stylesheet" type="text/css" />
    <link href="/Content/student-upgrade.css" rel="stylesheet" />
    <script src="/Content/fx.js"></script>
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <uc:Sidebar ID="Sidebar1" runat="server" />
            <main class="main">
                <div class="topbar">
                    <div class="lv-heading">
                        <span class="title"><asp:Literal ID="litCourseTitle" runat="server" Mode="Encode"></asp:Literal></span>
                        <span class="q-chip" id="chipBox" runat="server">Lesson <asp:Literal ID="litLessonNo" runat="server"></asp:Literal> of <asp:Literal ID="litLessonTotal" runat="server"></asp:Literal></span>
                    </div>
                    <div class="lv-top-right">
                        <span>Course progress: <strong><asp:Literal ID="litPct" runat="server"></asp:Literal>% Complete</strong></span>
                        <asp:HyperLink ID="lnkBackCourses" runat="server" NavigateUrl="~/pages/Student/myCourses.aspx" CssClass="btn-ghost">Back to Courses</asp:HyperLink>
                    </div>
                </div>

                <asp:Panel ID="pnlEmpty" runat="server" Visible="false" CssClass="result-box">
                    <h3>This course has no lessons yet.</h3>
                    <p style="margin-top:12px"><a href="myCourses.aspx" class="btn-solid">Back to courses</a></p>
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
                            <h2 class="lv-title"><asp:Literal ID="litLessonTitle" runat="server" Mode="Encode"></asp:Literal></h2>
                            <p class="lv-desc"><asp:Literal ID="litLessonDesc" runat="server" Mode="Encode"></asp:Literal></p>

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
                                    <span class="tutor-avatar"><asp:Literal ID="litTutorInitial" runat="server" Mode="Encode"></asp:Literal></span>
                                    <div>
                                        <div class="tutor-name"><asp:Literal ID="litTutorName" runat="server" Mode="Encode"></asp:Literal></div>
                                        <div class="tutor-role"><asp:Literal ID="litTutorRole" runat="server" Mode="Encode"></asp:Literal></div>
                                    </div>
                                </div>
                            </div>

                            <asp:HyperLink ID="lnkDiscuss" runat="server" CssClass="btn-ghost" Style="display:block;text-align:center;margin-bottom:22px">Open Lesson Discussion</asp:HyperLink>
                            <h3>Your progress</h3>
                            <div class="lv-bar"><div class="lv-bar-fill" id="barFill" runat="server"></div></div>
                            <small style="color:#6b7385"><asp:Literal ID="litDoneCount" runat="server"></asp:Literal> of <asp:Literal ID="litTotalCount" runat="server"></asp:Literal> lessons completed</small>

                            <!-- NotebookLM Course Assistant Widget Added Below Progress -->
                            <div style="margin-top: 30px; padding: 16px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px;">
                                <h4 style="margin-top: 0; color: #1e293b; font-size: 15px; margin-bottom: 8px;">📚 Course Study Notebook</h4>
                                <p style="font-size: 12px; color: #64748b; line-height: 1.4; margin-bottom: 12px;">Highlight any text on the left lesson panel to create and save a structured study note directly to this course notebook.</p>
                                <button type="button" id="sidebarCreateNoteBtn" disabled="disabled" class="btn-solid" style="width: 100%; text-align: center; background: #cbd5e1; cursor: not-allowed; border: none; padding: 8px; border-radius: 6px; font-weight: 600; font-size: 13px; color: white;">
                                    ✨ Create Note from Selection
                                </button>
                                <a href="savedNotes.aspx?course=<%= _courseId %>" style="display:block;margin-top:10px;font-size:13px;color:#4f46e5;font-weight:600">View my saved notes</a>
                            </div>
                        </div>

                    </div>
                </asp:Panel>
            </main>
        </div>

        <!-- Floating Popup Toolbar -->
        <div id="aiPopupBtn" style="display:none; position:absolute; z-index:1000; background:#4f46e5; color:white; padding:6px 12px; border-radius:6px; cursor:pointer; font-size:13px; font-weight:600; box-shadow:0 4px 12px rgba(0,0,0,0.15);">
            ✨ Create Note from Highlight
        </div>

        <!-- Interactive Modal Dialog -->
        <div id="aiModal" style="display:none; position:fixed; top:0; left:0; width:100%; height:100%; background:rgba(0,0,0,0.5); z-index:2000; align-items:center; justify-content:center;">
            <div style="background:white; width:500px; max-width:90%; padding:24px; border-radius:12px; box-shadow:0 10px 25px rgba(0,0,0,0.2);">
                <h3 style="margin-top:0; color:#1e293b;">EduCore AI - Course Study Note</h3>
                <p style="font-size:12px; color:#64748b; margin-bottom:4px;">Selected Highlight:</p>
                <div id="aiSelectedSnippet" style="font-size:13px; color:#334155; background:#f8fafc; padding:10px; border-left:4px solid #4f46e5; max-height:100px; overflow-y:auto; border-radius:4px;"></div>
                
                <p style="font-size:12px; color:#64748b; margin-top:12px; margin-bottom:4px;">Add specific instructions or questions (Optional):</p>
                <textarea id="aiUserQuery" placeholder="e.g., Explain this term simply with bullet points..." style="width:100%; height:60px; padding:8px; border:1px solid #cbd5e1; border-radius:6px; box-sizing:border-box;"></textarea>
                
                <div style="margin-top:14px; text-align:right;">
                    <button type="button" id="aiCloseModal" class="btn-ghost" style="margin-right:8px;">Cancel</button>
                    <button type="button" id="aiSubmitQuery" class="btn-solid" style="background:#4f46e5; color:white;">Save Note to Notebook</button>
                </div>
                <div id="aiResponseArea" style="margin-top:16px; padding:12px; background:#f1f5f9; border-radius:6px; display:none; font-size:14px; color:#334155; max-height:200px; overflow-y:auto;"></div>
            </div>
        </div>

        <script>
            let savedSelectedText = "";
            const currentCourseId = "<%= _courseId %>";
            const currentLessonId = "<%= _lessonId %>";

            // Listen for text highlights on the page
            document.addEventListener("mouseup", function (e) {
                const selection = window.getSelection();
                const text = selection.toString().trim();
                const popup = document.getElementById("aiPopupBtn");
                const modal = document.getElementById("aiModal");
                const sidebarBtn = document.getElementById("sidebarCreateNoteBtn");

                if (modal.style.display === "flex") return;

                const contentPanel = document.getElementById("<%= pnlContent.ClientID %>");
                if (text.length > 3 && contentPanel && contentPanel.contains(selection.anchorNode)) {
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
                    if (!popup.contains(e.target) && !sidebarBtn.contains(e.target)) {
                        popup.style.display = "none";
                    }
                }
            });

function openNoteModal() {
    if (!savedSelectedText) return;
    document.getElementById("aiSelectedSnippet").innerText = "\"" + savedSelectedText + "\"";
    document.getElementById("aiModal").style.display = "flex";
    document.getElementById("aiPopupBtn").style.display = "none";
}

document.getElementById("aiPopupBtn").addEventListener("click", openNoteModal);
document.getElementById("sidebarCreateNoteBtn").addEventListener("click", openNoteModal);

document.getElementById("aiCloseModal").addEventListener("click", function () {
    document.getElementById("aiModal").style.display = "none";
    document.getElementById("aiResponseArea").style.display = "none";
    document.getElementById("aiUserQuery").value = "";
});

document.getElementById("aiSubmitQuery").addEventListener("click", function () {
    const query = document.getElementById("aiUserQuery").value || "Summarize and explain this section.";
    const respArea = document.getElementById("aiResponseArea");
    respArea.style.display = "block";
    respArea.innerHTML = "Generating note and saving to course notebook...";

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
            if (data.success) {
                respArea.innerHTML = "<strong style='color:green;'>✓ Note successfully saved to course notebook!</strong><br/><br/>" + data.answer.replace(/\n/g, '<br/>');
            } else {
                respArea.innerHTML = "<strong style='color:red;'>" + data.answer + "</strong>";
            }
        })
        .catch(err => {
            respArea.innerHTML = "Error connecting to AI service.";
        });
});
        </script>
    </form>
</body>
</html>