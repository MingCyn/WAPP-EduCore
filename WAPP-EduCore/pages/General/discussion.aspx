<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="discussion.aspx.cs" Inherits="WAPP_EduCore.pages.General.WebForm3" %>
<%@ Register Src="~/pages/Shared/Sidebar.ascx" TagName="Sidebar" TagPrefix="uc" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Discussion Forum</title>
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/student.css" rel="stylesheet" type="text/css" />
    <style>
        /* Discussion page specific styles */
        .forum-wrap{display:grid;grid-template-columns:1fr 320px;gap:24px}
        .tabs{display:flex;gap:10px;margin-bottom:16px}
        .tab{padding:8px 12px;background:#fff;border-radius:20px;border:1px solid #eef0f3}
        .tab.active{background:#2f4f8a;color:#fff}

        .post{background:#fff;border-radius:10px;padding:16px;border:1px solid #eef2ff;margin-bottom:14px}
        .post.featured{border-color:#2f4f8a}
        .post .author{display:flex;align-items:center;gap:12px;margin-bottom:8px}
        .avatar-sm{width:36px;height:36px;border-radius:50%;background:#e9eef6;display:inline-block}
        .meta{color:#7b8697;font-size:13px}

        .reply{background:#f7f7f8;padding:10px;border-radius:6px;margin-top:8px}

        .right .card{margin-bottom:12px}
        .post-form input, .post-form textarea{width:100%;padding:10px;border-radius:8px;border:1px solid #ececec;background:#fbfbfb}
        .post-form button{background:#2f4f8a;color:#fff;padding:8px 12px;border-radius:8px;border:none}

        @media(max-width:1000px){.forum-wrap{grid-template-columns:1fr}.right{order:2}}
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <uc:Sidebar ID="Sidebar1" runat="server" />

            <main class="main">
                <div class="topbar">
                    <div>
                        <h1>Discussion Forum <span style="background:#fff2d9;padding:4px 8px;border-radius:6px;font-size:13px;margin-left:8px">Lesson 2.3</span></h1>
                        <p style="color:#7b8697">Topic: Arrays &amp; Linked Lists</p>
                    </div>
                    <div>
                        <span class="avatar">DM</span>
                    </div>
                </div>

                <div class="forum-wrap">
                    <div>
                        <div class="tabs">
                            <div class="tab">All Posts</div>
                            <div class="tab active">Discussed in Lesson</div>
                            <div class="tab">Questions</div>
                            <div class="tab">Announcements</div>
                        </div>

                        <div class="post featured">
                            <div class="author">
                                <div class="avatar-sm"></div>
                                <div>
                                    <div style="font-weight:600">Prof. Elena Vance</div>
                                    <div class="meta">Tutor · 2 hours ago</div>
                                </div>
                            </div>
                            <h3>Sequential indexing bottlenecks in sparse matrices</h3>
                            <p>If we traverse a linked list sequentially, we trigger consecutive address misses. In comparison, block allocations are highly cache-friendly.</p>

                            <div class="reply">
                                <strong>Priya Sharma</strong>
                                <div class="meta">Understood! Is this why we prefer compressed sparse row (CSR) formats?</div>
                            </div>
                            <div class="reply">
                                <strong>Prof. Elena Vance</strong>
                                <div class="meta">Exactly, CSR forces contiguous memory layouts for sparse indices.</div>
                            </div>
                        </div>

                        <div class="post">
                            <div class="author">
                                <div class="avatar-sm"></div>
                                <div>
                                    <div style="font-weight:600">Felicia Elase</div>
                                    <div class="meta">Student · 4 hours ago</div>
                                </div>
                            </div>
                            <h3>Complexity profile differences for double-pointer setups</h3>
                            <div class="meta">Replies: 4 &nbsp;&nbsp; Likes: 12</div>
                        </div>
                    </div>

                    <aside class="right">
                        <div class="card post-form">
                            <h4>Post New Discussion</h4>
                            <label>Topic Title</label>
                            <input placeholder="Enter thread title..." />
                            <label style="margin-top:8px">Content</label>
                            <textarea rows="5" placeholder="Explain details clearly..."></textarea>
                            <div style="display:flex;gap:8px;margin-top:8px"><button type="button">Attach File</button><button type="button">Post Topic</button></div>
                        </div>

                        <div class="card">
                            <h4>Active Participants</h4>
                            <div style="display:flex;gap:8px;margin-top:8px">
                                <div class="avatar-sm"></div>
                                <div class="avatar-sm"></div>
                                <div class="avatar-sm"></div>
                                <div class="avatar-sm"></div>
                            </div>
                        </div>
                    </aside>
                </div>
            </main>
        </div>
    </form>

    <script>
        // mark 'Grades' menu item active in shared sidebar (Grades now opens this discussion session)
        (function(){
            var items = document.querySelectorAll('.sidebar .menu-item');
            items.forEach(function(a){ if(a.textContent.trim()==='Grades') { a.classList.add('active'); } });
        })();
    </script>
</body>
</html>
