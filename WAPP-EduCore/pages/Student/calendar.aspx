<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="calendar.aspx.cs" Inherits="WAPP_EduCore.pages.Student.calendar" %>
<%@ Register Src="~/pages/Shared/Sidebar.ascx" TagName="Sidebar" TagPrefix="uc" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Calendar</title>
    <link href="../../Content/admin.css" rel="stylesheet" type="text/css" />
    <link href="../../Content/student.css" rel="stylesheet" type="text/css" />
    <style>
        /* Calendar specific styles */
        .calendar-wrap{display:grid;grid-template-columns:1fr 320px;gap:24px}
        .calendar-panel{background:transparent}
        .calendar-header{display:flex;align-items:center;gap:12px;margin-bottom:14px}
        .date-title{font-size:20px;font-weight:600}
        .date-nav button{padding:6px 8px;border-radius:6px;border:1px solid #e6e6e6;background:#fff}
        .view-toggle{margin-left:auto;display:flex;gap:8px}
        .view-toggle button{padding:8px 12px;border-radius:8px;border:1px solid #e6e6e6;background:#fff}

        .week-days{display:flex;gap:14px;margin-bottom:12px}
        .weekday{padding:8px 12px;border:1px solid transparent;border-radius:8px;background:#fff}

        .grid-calendar{display:grid;grid-template-columns:80px repeat(7,1fr);gap:12px}
        .time-col{display:flex;flex-direction:column;gap:12px}
        .time-cell{height:48px;color:#9aa6b8;font-size:13px;padding-right:8px;text-align:right}
        .day-cell{background:#fff;border-radius:8px;min-height:48px;border:1px solid #f1f3f5}

        .event{padding:8px 10px;border-radius:6px;color:#fff;font-size:13px;margin:4px;display:inline-block}
        .event.blue{background:#264a8a}
        .event.orange{background:#d9a863;color:#2b2b2b}

        .side-card{background:#fff;padding:16px;border-radius:10px}
        .upcoming .item{background:#fbf3e8;padding:10px;border-radius:8px;margin-bottom:10px}
        .add-btn{display:block;margin-top:12px;background:#2f4f8a;color:#fff;padding:10px;border-radius:8px;text-align:center;text-decoration:none}

        @media(max-width:1000px){.calendar-wrap{grid-template-columns:1fr}.view-toggle{display:none}}
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="app">
            <uc:Sidebar ID="Sidebar1" runat="server" />

            <main class="main">
                <div class="topbar">
                    <div>
                        <h1>Calendar</h1>
                        <p>August 25-31, 2026</p>
                    </div>
                    <div>
                        <span class="avatar">PS</span>
                    </div>
                </div>

                <div class="calendar-wrap">
                    <div class="calendar-panel">
                        <div class="calendar-header">
                            <div class="date-title">August 25-31, 2026</div>
                            <div class="date-nav"><button>&lt;</button><button>&gt;</button></div>
                            <div class="view-toggle">
                                <button>Month</button>
                                <button style="background:#2f4f8a;color:#fff">Week</button>
                                <button>Day</button>
                            </div>
                        </div>

                        <div class="week-days">
                            <div class="weekday">Mon 25</div>
                            <div class="weekday">Tue 26</div>
                            <div class="weekday">Wed 27</div>
                            <div class="weekday">Thu 28</div>
                            <div class="weekday">Fri 29</div>
                            <div class="weekday">Sat 30</div>
                            <div class="weekday">Sun 31</div>
                        </div>

                        <div class="grid-calendar">
                            <div class="time-col">
                                <div style="height:40px"></div>
                                <div class="time-cell">8 AM</div>
                                <div class="time-cell">10 AM</div>
                                <div class="time-cell">12 PM</div>
                                <div class="time-cell">2 PM</div>
                                <div class="time-cell">4 PM</div>
                                <div class="time-cell">6 PM</div>
                                <div class="time-cell">8 PM</div>
                            </div>

                            <!-- seven day columns -->
                            <div class="day-cell"></div>
                            <div class="day-cell"></div>
                            <div class="day-cell"></div>
                            <div class="day-cell">
                                <div class="event blue">Live: Algorithms<br /><small style="color:#e6eefc">10:00 - 11:30</small></div>
                            </div>
                            <div class="day-cell"></div>
                            <div class="day-cell">
                                <div class="event orange">Lab Due: BST<br /><small style="color:#3b2a12">2:00 PM</small></div>
                            </div>
                            <div class="day-cell">
                                <div class="event blue">Self-Study: Design<br /><small style="color:#e6eefc">12:00 - 1:30</small></div>
                            </div>
                        </div>
                    </div>

                    <aside>
                        <div class="side-card">
                            <h4>Upcoming Events</h4>
                            <div class="upcoming">
                                <div class="item"><strong>Live: Complex Trees</strong><div style="font-size:13px;color:#7b8697">Aug 26, 10:00 AM</div></div>
                                <div class="item"><strong>Assignment: Design proposal</strong><div style="font-size:13px;color:#7b8697">Aug 28, 4:00 PM</div></div>
                                <div class="item"><strong>Algorithms Midterm</strong><div style="font-size:13px;color:#7b8697">Sep 02, 9:00 AM</div></div>
                            </div>
                            <a class="add-btn" href="#">+ Add Event</a>
                        </div>
                    </aside>
                </div>
            </main>
        </div>
    </form>
</body>
</html>
