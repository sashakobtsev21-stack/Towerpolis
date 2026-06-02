// Towerpolis — direct BlenderMCP socket client (no MCP server / no session-root needed).
// The BlenderMCP addon listens on raw TCP 127.0.0.1:9876 and accepts one complete JSON
// command per connection: {type, params}. We send execute_code with bpy Python.
//   node scripts/blender_send.js --code scripts/blender_build_blocks.py
//   node scripts/blender_send.js --raw '{"type":"get_scene_info","params":{}}'
// Prints the captured stdout (for execute_code) or the JSON result.
const net = require("net");
const fs = require("fs");
const args = process.argv.slice(2);
let cmd;
const ci = args.indexOf("--code");
const ri = args.indexOf("--raw");
if (ci >= 0) cmd = { type: "execute_code", params: { code: fs.readFileSync(args[ci + 1], "utf8") } };
else if (ri >= 0) cmd = JSON.parse(args[ri + 1]);
else { console.error("usage: --code <file.py> | --raw <json>"); process.exit(1); }

const host = process.env.BLENDER_HOST || "127.0.0.1";
const port = +(process.env.BLENDER_PORT || 9876);
const s = net.connect(port, host, () => s.write(JSON.stringify(cmd)));
let buf = "";
s.setTimeout(180000);
s.on("data", d => {
  buf += d.toString("utf8");
  try {
    const o = JSON.parse(buf);
    if (o.status === "success") {
      const r = o.result;
      if (r && typeof r === "object" && "result" in r) process.stdout.write(String(r.result));
      else process.stdout.write(JSON.stringify(r, null, 2));
      process.exitCode = 0;
    } else {
      process.stderr.write("BLENDER ERROR: " + (o.message || JSON.stringify(o)));
      process.exitCode = 2;
    }
    s.end();
  } catch (e) { /* keep accumulating until valid JSON */ }
});
s.on("timeout", () => { process.stderr.write("TIMEOUT"); s.destroy(); process.exitCode = 3; });
s.on("error", e => { process.stderr.write("SOCKET ERROR: " + e.message); process.exitCode = 4; });
