namespace ServerScreenViewer;

internal static class HtmlPages
{
    public static string Login(string? error = null) => $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width,initial-scale=1">
  <title>Server Screen Viewer</title>
  <style>
    :root{color-scheme:dark;font-family:Segoe UI,Arial,sans-serif}body{margin:0;min-height:100vh;display:grid;place-items:center;background:#10131a;color:#f3f6fb}.card{width:min(420px,calc(100% - 40px));padding:28px;border:1px solid #303848;border-radius:16px;background:#171c25;box-shadow:0 16px 50px #0007}h1{font-size:1.45rem;margin:0 0 8px}.muted{color:#aeb8ca}.error{padding:10px;border-radius:8px;background:#5b1f2a;color:#ffdce2;margin:16px 0}label{display:block;margin:20px 0 8px}input{box-sizing:border-box;width:100%;padding:12px;border:1px solid #45516a;border-radius:8px;background:#0e1219;color:#fff}button{width:100%;margin-top:16px;padding:12px;border:0;border-radius:8px;background:#4f8cff;color:#fff;font-weight:650;cursor:pointer}
  </style>
</head>
<body>
  <main class="card">
    <h1>Server Screen Viewer</h1>
    <p class="muted">Enter the access key shown in the host application.</p>
    {{(string.IsNullOrEmpty(error) ? string.Empty : $"<div class=\"error\">{System.Net.WebUtility.HtmlEncode(error)}</div>")}}
    <form method="post" action="/login">
      <label for="key">Access key</label>
      <input id="key" name="key" type="password" autocomplete="current-password" required autofocus>
      <button type="submit">View screen</button>
    </form>
  </main>
</body>
</html>
""";

    public static string Viewer(int refreshMs) => $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width,initial-scale=1">
  <title>Server Screen Viewer</title>
  <style>
    :root{color-scheme:dark;font-family:Segoe UI,Arial,sans-serif}*{box-sizing:border-box}body{margin:0;background:#080a0f;color:#eef2f9}header{height:52px;display:flex;align-items:center;gap:12px;padding:0 16px;background:#141925;border-bottom:1px solid #293144}h1{font-size:1rem;margin:0}.status{margin-left:auto;color:#9fb0c9;font-size:.9rem}main{height:calc(100vh - 52px);display:grid;place-items:center;overflow:auto;padding:12px}img{max-width:100%;max-height:100%;object-fit:contain;background:#000;box-shadow:0 0 0 1px #31394b}button{padding:7px 11px;border:1px solid #46516a;border-radius:7px;background:#232b3a;color:#fff;cursor:pointer}
  </style>
</head>
<body>
  <header><h1>Server Screen Viewer</h1><button id="full">Full screen</button><button id="logout">Sign out</button><span id="status" class="status">Connecting…</span></header>
  <main><img id="screen" alt="Live server screen"></main>
  <script>
    const image=document.getElementById('screen');
    const status=document.getElementById('status');
    let stopped=false;
    function next(){if(stopped)return;setTimeout(load,{{refreshMs}})}
    function load(){
      image.onload=()=>{status.textContent='Live · '+new Date().toLocaleTimeString();URL.revokeObjectURL(image.src);next()};
      image.onerror=()=>{status.textContent='Connection lost—retrying';next()};
      fetch('/api/snapshot',{cache:'no-store'}).then(r=>{if(r.status===401){location.reload();throw new Error('signed out')}if(!r.ok)throw new Error('capture failed');return r.blob()}).then(b=>{image.src=URL.createObjectURL(b)}).catch(()=>next());
    }
    document.getElementById('full').onclick=()=>document.documentElement.requestFullscreen();
    document.getElementById('logout').onclick=()=>fetch('/logout',{method:'POST'}).finally(()=>location.reload());
    document.addEventListener('visibilitychange',()=>{stopped=document.hidden;if(!stopped)load()});
    load();
  </script>
</body>
</html>
""";
}
