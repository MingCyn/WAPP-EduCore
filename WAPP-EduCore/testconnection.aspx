<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="testconnection.aspx.cs" Inherits="WAPP_EduCore.testconnection" %>

<!DOCTYPE html>
<html>
<head runat="server">
    <title>Supabase Connection Test</title>
    <style>
        body { font-family: 'Inter', Arial, sans-serif; background-color: #F8F8F6; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; }
        .card { background: white; padding: 40px; border-radius: 12px; box-shadow: 0 4px 12px rgba(0,0,0,0.05); text-align: center; max-width: 500px; width: 100%; }
        h2 { color: #0F172A; margin-bottom: 20px; }
        .status-msg { font-size: 16px; font-weight: 500; margin-top: 10px; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="card">
            <h2>Supabase Connection Test</h2>
            <asp:Label ID="lblStatus" runat="server" CssClass="status-msg" Text="Testing connection..."></asp:Label>
        </div>
    </form>
</body>
</html>