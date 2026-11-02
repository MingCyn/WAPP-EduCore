<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="messages.aspx.cs" Inherits="WAPP_EduCore.pages.Student.messages" %>

<%@ Register Src="~/pages/Shared/Sidebar.ascx" TagName="Sidebar" TagPrefix="uc" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Discussion Forum</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Merriweather:wght@400;700&display=swap"
        rel="stylesheet" />
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/student.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/messages.css" rel="stylesheet" type="text/css" />
    <link href="/Content/student-upgrade.css" rel="stylesheet" />
    <script src="/Content/fx.js"></script>
    <style>
        /* ===== NEW: Edit / Delete own post ===== */

        /* "Your post" pill next to the author's name */
        .fmx-you {
            display: inline-block;
            margin-left: 6px;
            padding: 2px 8px;
            border-radius: 20px;
            background: #e0e7ff;
            color: #243f86;
            font-size: 11px;
            font-weight: 700;
        }

        /* Edit / Delete buttons under the selected post */
        .fmx-actions {
            display: flex;
            gap: 10px;
            margin: 14px 0 4px;
            padding-top: 12px;
            border-top: 1px solid #eceae6;
        }

        .fmx-act {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            padding: 7px 14px;
            border: 1px solid #d5dae5;
            border-radius: 8px;
            background: #fff;
            color: #243f86;
            font-size: 13px;
            font-weight: 600;
            cursor: pointer;
            transition: background .15s ease, border-color .15s ease, transform .15s ease;
        }

            .fmx-act:hover {
                background: #f3f5fb;
                border-color: #243f86;
            }

            .fmx-act:active {
                transform: scale(.97);
            }

            .fmx-act.danger {
                color: #d64545;
            }

                .fmx-act.danger:hover {
                    background: #fdf1f1;
                    border-color: #d64545;
                }

        /* Popups (same look as the Course Overview / Calendar popups) */
        .fmx-overlay {
            position: fixed;
            inset: 0;
            background: rgba(15,26,70,.55);
            z-index: 9999;
            display: none;
            align-items: center;
            justify-content: center;
            padding: 16px;
            box-sizing: border-box;
        }

            .fmx-overlay.open {
                display: flex;
            }

        .fmx-box {
            background: #fff;
            border-radius: 14px;
            padding: 28px;
            width: 100%;
            max-width: 520px;
            max-height: 92vh;
            overflow-y: auto;
            box-sizing: border-box;
            box-shadow: 0 20px 50px rgba(0,0,0,.25);
        }

            .fmx-box.center {
                max-width: 420px;
                text-align: center;
            }

            .fmx-box h3 {
                margin: 0 0 6px;
                font-family: 'Merriweather', Georgia, serif;
                font-size: 20px;
                color: #1a2550;
            }

            .fmx-box p.sub {
                margin: 0 0 16px;
                font-size: 13.5px;
                line-height: 1.5;
                color: #6b7385;
            }

            .fmx-box.center p {
                margin: 0 0 22px;
                font-size: 14px;
                line-height: 1.55;
                color: #4a5468;
            }

        .fmx-icon {
            display: flex;
            align-items: center;
            justify-content: center;
            width: 64px;
            height: 64px;
            margin: 0 auto 16px;
            border-radius: 50%;
            font-size: 30px;
            font-weight: 700;
            background: rgba(16,185,129,.12);
            color: #10b981;
        }

            .fmx-icon.warn {
                background: rgba(255,112,0,.12);
                color: #ff7000;
            }

            .fmx-icon.error {
                background: rgba(214,69,69,.12);
                color: #d64545;
            }

        .fmx-label {
            display: flex;
            justify-content: space-between;
            align-items: baseline;
            margin: 14px 0 6px;
            font-size: 13px;
            font-weight: 600;
            color: #16223b;
        }

            .fmx-label small {
                font-weight: 500;
                color: #9aa6b8;
            }

        .fmx-field {
            width: 100%;
            padding: 10px 12px;
            border: 1px solid #cbd5e1;
            border-radius: 8px;
            box-sizing: border-box;
            font: inherit;
            font-size: 14px;
            color: #1e293b;
            background: #fff;
        }

            .fmx-field:focus {
                outline: none;
                border-color: #243f86;
                box-shadow: 0 0 0 3px rgba(36,63,134,.12);
            }

        textarea.fmx-field {
            resize: vertical;
            min-height: 130px;
        }

        .fmx-err {
            margin-top: 12px;
            padding: 9px 12px;
            border-radius: 8px;
            background: #fdf1f1;
            border: 1px solid #f3c4c4;
            color: #b91c1c;
            font-size: 13px;
        }

            .fmx-err:empty {
                display: none;
            }

        .fmx-btnrow {
            display: flex;
            gap: 12px;
            justify-content: flex-end;
            margin-top: 20px;
        }

        .fmx-box.center .fmx-btnrow {
            justify-content: center;
            margin-top: 0;
        }

        .fmx-btn {
            padding: 10px 26px;
            border-radius: 8px;
            border: 1px solid #e0e0e0;
            background: #fff;
            color: #16223b;
            font-size: 14px;
            font-weight: 600;
            cursor: pointer;
            transition: background .2s ease, transform .2s ease;
        }

            .fmx-btn:hover {
                background: #f5f5f5;
            }

            .fmx-btn:active {
                transform: scale(.97);
            }

            .fmx-btn.primary {
                background: #243f86;
                border-color: #243f86;
                color: #fff;
            }

                .fmx-btn.primary:hover {
                    background: #1b3270;
                }

            .fmx-btn.danger {
                background: #d64545;
                border-color: #d64545;
                color: #fff;
            }

                .fmx-btn.danger:hover {
                    background: #b83a3a;
                }

            .fmx-btn:disabled {
                opacity: .6;
                cursor: not-allowed;
            }

        @media (max-width: 520px) {
            .fmx-box {
                padding: 22px 18px;
            }

            .fmx-btnrow {
                flex-direction: column-reverse;
            }

            .fmx-btn {
                width: 100%;
            }
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <uc:Sidebar ID="Sidebar1" runat="server" />

            <main class="main">
                <div class="topbar">
                    <div class="fm-head-left">
                        <span class="title">Discussion Forum</span>
                        <asp:Literal ID="litChip" runat="server" Mode="PassThrough"></asp:Literal>
                    </div>
                    <div class="fm-head-right">
                        <asp:Literal ID="litTopic" runat="server" Mode="PassThrough"></asp:Literal>
                    </div>
                </div>

                <!-- Guest Preview Banner -->
                <asp:Panel ID="pnlGuestNotice" runat="server" Visible="false" CssClass="fm-empty-box"
                    Style="margin-bottom: 15px; background: #eff6ff; color: #1e40af; border: 1px solid #bfdbfe;">
                    Showing a preview of the 3 newest community discussions. <a href="~/auth/signup.aspx"
                        runat="server">Sign up or log in</a> to view all discussions and participate.
                </asp:Panel>

                <asp:Panel ID="pnlNoCourses" runat="server" Visible="false" CssClass="fm-empty-box">
                    You are not enrolled in any course yet, so there are no discussions to show.
                   
                    <br />
                    <a href="myCourses.aspx">Browse courses</a>
                </asp:Panel>

                <asp:Panel ID="pnlForum" runat="server">
                    <div class="fm-tabs">
                        <asp:Literal ID="litTabs" runat="server" Mode="PassThrough"></asp:Literal>
                    </div>

                    <div class="fm-layout">
                        <div>
                            <asp:Literal ID="litBefore" runat="server" Mode="PassThrough"></asp:Literal>

                            <asp:Panel ID="pnlSelected" runat="server" Visible="false" CssClass="fm-card selected">
                                <asp:Literal ID="litSelTop" runat="server" Mode="PassThrough"></asp:Literal>
                                <div class="fm-replies">
                                    <asp:Literal ID="litReplies" runat="server" Mode="PassThrough"></asp:Literal>
                                </div>
                                <asp:Label ID="lblReplyError" runat="server" CssClass="fm-error" Visible="false"></asp:Label>
                                <div class="fm-replyform">
                                    <asp:TextBox ID="txtReply" runat="server" TextMode="MultiLine" Rows="2"
                                        placeholder="Write a reply..."></asp:TextBox>
                                    <asp:Button ID="btnReply" runat="server" Text="Reply" CssClass="fm-btn small"
                                        OnClick="btnReply_Click" />
                                </div>
                            </asp:Panel>

                            <asp:Literal ID="litAfter" runat="server" Mode="PassThrough"></asp:Literal>

                            <asp:Panel ID="pnlNoPosts" runat="server" Visible="false" CssClass="fm-empty-box">
                                No discussions here yet. Start one using the form.
                           
                            </asp:Panel>
                        </div>

                        <div>
                            <div class="fm-box">
                                <h3>Post New Discussion</h3>
                                <asp:Label ID="lblPostError" runat="server" CssClass="fm-error" Visible="false"></asp:Label>

                                <label for="txtTitle">Topic title</label>
                                <asp:TextBox ID="txtTitle" runat="server" MaxLength="150" placeholder="Enter thread title..."></asp:TextBox>

                                <asp:Panel ID="pnlCoursePick" runat="server">
                                    <label for="ddlCourse">Course</label>
                                    <asp:DropDownList ID="ddlCourse" runat="server"></asp:DropDownList>
                                </asp:Panel>

                                <label for="ddlType">Type</label>
                                <asp:DropDownList ID="ddlType" runat="server">
                                    <asp:ListItem Value="Discussion">Discussion</asp:ListItem>
                                    <asp:ListItem Value="Question">Question</asp:ListItem>
                                </asp:DropDownList>

                                <label for="txtContent">Content</label>
                                <asp:TextBox ID="txtContent" runat="server" TextMode="MultiLine" Rows="4"
                                    placeholder="Explain details clearly..."></asp:TextBox>

                                <asp:Button ID="btnPost" runat="server" Text="Post Topic" CssClass="fm-btn" OnClick="btnPost_Click" />
                            </div>

                            <div class="fm-box">
                                <h3>Active Participants</h3>
                                <asp:Literal ID="litPeople" runat="server" Mode="PassThrough"></asp:Literal>
                            </div>
                        </div>
                    </div>
                </asp:Panel>
            </main>
        </div>

        <!-- Not Logged In Modal Popup -->
        <div class="modal-back" id="loginPromptModal">
            <div class="modal-box" style="text-align: center; padding: 32px; max-width: 440px;">
                <div style="font-size: 40px; margin-bottom: 12px;">🔒</div>
                <h3 style="font-family: 'Merriweather', Georgia, serif; font-size: 20px; color: #111c3d;
                    margin-bottom: 8px;">Not logged in?</h3>
                <p style="font-size: 14px; color: #4a5468; margin-bottom: 24px; line-height: 1.5;">
                    Login or create an account to unlock all functions, post discussions, track your
                    progress, and manage your schedule!
               
                </p>
                <div style="display: flex; gap: 12px; justify-content: center;">
                    <a href="<%= ResolveUrl("~/auth/login.aspx") %>" class="cl-btn-solid" style="text-decoration: none;
                        padding: 10px 24px; display: inline-flex; align-items: center; justify-content: center;">
                        Log In</a>
                    <a href="<%= ResolveUrl("~/auth/signup.aspx") %>" class="cl-btn-ghost" style="text-decoration: none;
                        padding: 10px 24px; display: inline-flex; align-items: center; justify-content: center;">
                        Sign Up</a>
                    <button type="button" class="cl-btn-ghost" onclick="closeLoginPrompt()" style="padding: 10px 16px;">
                        Cancel</button>
                </div>
            </div>
        </div>

        <!-- NEW: Edit post popup -->
        <div class="fmx-overlay" id="fmxEditModal" onclick="if (event.target === this) fmxCloseEdit();">
            <div class="fmx-box" role="dialog" aria-modal="true" aria-labelledby="fmxEditTitle">
                <h3 id="fmxEditTitle">Edit your post</h3>
                <p class="sub">Update your topic below. Replies from other students stay attached to
                    it.</p>

                <div class="fmx-label">
                    <span>Topic title</span><small id="fmxTitleCount">0 / 150</small>
                </div>
                <asp:TextBox ID="txtEditTitle" runat="server" CssClass="fmx-field" MaxLength="150"
                    placeholder="Enter thread title..."></asp:TextBox>

                <div class="fmx-label"><span>Type</span></div>
                <asp:DropDownList ID="ddlEditType" runat="server" CssClass="fmx-field">
                    <asp:ListItem Value="Discussion">Discussion</asp:ListItem>
                    <asp:ListItem Value="Question">Question</asp:ListItem>
                </asp:DropDownList>

                <div class="fmx-label">
                    <span>Content</span><small id="fmxContentCount">0 / 2000</small>
                </div>
                <asp:TextBox ID="txtEditContent" runat="server" TextMode="MultiLine" Rows="6"
                    CssClass="fmx-field" placeholder="Explain details clearly..."></asp:TextBox>

                <div id="fmxEditErr" class="fmx-err" runat="server"></div>

                <div class="fmx-btnrow">
                    <button type="button" class="fmx-btn" onclick="fmxCloseEdit()">Cancel</button>
                    <asp:Button ID="btnSaveEdit" runat="server" Text="Save Changes" CssClass="fmx-btn primary"
                        OnClick="btnSaveEdit_Click" OnClientClick="return fmxSaveClick(this);" />
                </div>
            </div>
        </div>

        <!-- NEW: Delete post popup (replaces the browser confirm box) -->
        <div class="fmx-overlay" id="fmxDeleteModal" onclick="if (event.target === this) fmxCloseDelete();">
            <div class="fmx-box center" role="dialog" aria-modal="true" aria-labelledby="fmxDeleteTitle">
                <div class="fmx-icon error">
                    <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor"
                        stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
                        <polyline points="3 6 5 6 21 6" />
                        <path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6" />
                        <path d="M10 11v6" />
                        <path d="M14 11v6" />
                        <path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2" />
                    </svg>
                </div>
                <h3 id="fmxDeleteTitle">Delete this post?</h3>
                <p id="fmxDeleteMsg"></p>
                <div class="fmx-btnrow">
                    <button type="button" class="fmx-btn" id="fmxDeleteCancel" onclick="fmxCloseDelete()">
                        Cancel</button>
                    <asp:Button ID="btnConfirmDelete" runat="server" Text="Delete" CssClass="fmx-btn danger"
                        OnClick="btnConfirmDelete_Click" OnClientClick="return fmxDeleteClick(this);" />
                </div>
            </div>
        </div>

        <!-- NEW: Notice popup (success / error), replaces alert() -->
        <div class="fmx-overlay" id="fmxNoticeModal" onclick="if (event.target === this) fmxCloseNotice();">
            <div class="fmx-box center" role="dialog" aria-modal="true" aria-labelledby="fmxNoticeTitle">
                <div class="fmx-icon" id="fmxNoticeIcon">&#10003;</div>
                <h3 id="fmxNoticeTitle"></h3>
                <p id="fmxNoticeMsg"></p>
                <div class="fmx-btnrow">
                    <button type="button" class="fmx-btn primary" onclick="fmxCloseNotice()" style="min-width: 110px;">
                        OK</button>
                </div>
            </div>
        </div>

        <script>
            if (typeof openLoginPrompt !== 'function') {
                window.openLoginPrompt = function () {
                    var modal = document.getElementById('loginPromptModal');
                    if (modal) modal.classList.add('open');
                };
            }
            if (typeof closeLoginPrompt !== 'function') {
                window.closeLoginPrompt = function () {
                    var modal = document.getElementById('loginPromptModal');
                    if (modal) modal.classList.remove('open');
                };
            }
        </script>

        <script>
            /* ================= Edit / Delete own post ================= */
            var fmxIds = {
                title: '<%= txtEditTitle.ClientID %>',
                type: '<%= ddlEditType.ClientID %>',
                content: '<%= txtEditContent.ClientID %>',
                err: '<%= fmxEditErr.ClientID %>'
            };

            function fmxEl(id) { return document.getElementById(id); }

            function fmxUpdateCounts() {
                var t = fmxEl(fmxIds.title), c = fmxEl(fmxIds.content);
                if (t) fmxEl('fmxTitleCount').textContent = t.value.length + ' / 150';
                if (c) {
                    var len = c.value.length;
                    var el = fmxEl('fmxContentCount');
                    el.textContent = len + ' / 2000';
                    el.style.color = len > 2000 ? '#d64545' : '';
                }
            }

            // Edit button on the post: fill the popup with the post's current values, then open it
            function fmxOpenEdit(btn) {
                fmxEl(fmxIds.title).value = btn.getAttribute('data-title') || '';
                fmxEl(fmxIds.content).value = btn.getAttribute('data-content') || '';
                var typeEl = fmxEl(fmxIds.type);
                var type = btn.getAttribute('data-type') || 'Discussion';
                typeEl.value = (type === 'Question') ? 'Question' : 'Discussion';
                fmxEl(fmxIds.err).textContent = '';
                fmxUpdateCounts();
                fmxShowEdit();
            }

            // Opens the popup WITHOUT touching the fields (used after a server error so the student keeps what they typed)
            function fmxShowEdit() {
                fmxUpdateCounts();
                fmxEl('fmxEditModal').classList.add('open');
                var t = fmxEl(fmxIds.title);
                if (t) t.focus();
            }

            function fmxCloseEdit() { fmxEl('fmxEditModal').classList.remove('open'); }

            // Checks the form before sending it to the server
            function fmxSaveClick(btn) {
                var title = fmxEl(fmxIds.title).value.trim();
                var content = fmxEl(fmxIds.content).value.trim();
                var err = fmxEl(fmxIds.err);

                if (!title) { err.textContent = 'Please enter a topic title.'; return false; }
                if (title.length > 150) { err.textContent = 'The title is too long (150 characters at most).'; return false; }
                if (!content) { err.textContent = 'Please write something in the content box.'; return false; }
                if (content.length > 2000) { err.textContent = 'The content is too long (2000 characters at most).'; return false; }

                err.textContent = '';
                fmxBusy(btn, 'Saving...');
                return true;
            }

            // Delete button on the post: show the confirmation popup
            function fmxAskDelete(btn) {
                var title = btn.getAttribute('data-title') || '';
                var replies = parseInt(btn.getAttribute('data-replies') || '0', 10) || 0;

                var msg = '\u201C' + title + '\u201D ';
                if (replies > 0) {
                    msg += 'and its ' + replies + (replies === 1 ? ' reply' : ' replies') +
                        ' will be permanently deleted. This can\u2019t be undone.';
                } else {
                    msg += 'will be permanently deleted. This can\u2019t be undone.';
                }
                fmxEl('fmxDeleteMsg').textContent = msg;
                fmxEl('fmxDeleteModal').classList.add('open');
                fmxEl('fmxDeleteCancel').focus();
            }

            function fmxCloseDelete() { fmxEl('fmxDeleteModal').classList.remove('open'); }

            function fmxDeleteClick(btn) {
                fmxBusy(btn, 'Deleting...');
                return true;
            }

            // Stops double clicks. It waits one tick so the browser still sends the button with the form.
            function fmxBusy(btn, text) {
                setTimeout(function () {
                    btn.disabled = true;
                    btn.value = text;
                }, 0);
            }

            // Styled replacement for alert(). kind = 'success' | 'warn' | 'error'
            function fmxNotice(kind, title, message) {
                var icons = { success: '\u2713', warn: '!', error: '\u2715' };
                var icon = fmxEl('fmxNoticeIcon');
                icon.className = 'fmx-icon ' + (kind === 'success' ? '' : kind);
                icon.textContent = icons[kind] || icons.success;
                fmxEl('fmxNoticeTitle').textContent = title;
                fmxEl('fmxNoticeMsg').textContent = message;
                fmxEl('fmxNoticeModal').classList.add('open');
            }

            function fmxCloseNotice() { fmxEl('fmxNoticeModal').classList.remove('open'); }

            (function () {
                var t = fmxEl(fmxIds.title), c = fmxEl(fmxIds.content);
                if (t) t.addEventListener('input', fmxUpdateCounts);
                if (c) c.addEventListener('input', fmxUpdateCounts);
            })();

            document.addEventListener('keydown', function (e) {
                if (e.key === 'Escape') {
                    fmxCloseEdit();
                    fmxCloseDelete();
                    fmxCloseNotice();
                    if (typeof closeLoginPrompt === 'function') closeLoginPrompt();
                }
            });
        </script>
    </form>
</body>
</html>
