# Towerpolis — one-time agent / plugin / MCP activation (Windows PowerShell)
# Run from the repo root:  ./scripts/setup-agents.ps1
# Safe to re-run. Installs the game-relevant Ruflo plugins and Git LFS.
# See docs/AGENT_ORCHESTRATION.md for what each does.

$ErrorActionPreference = 'Stop'
Write-Host "== Towerpolis agent setup ==" -ForegroundColor Cyan

# 1) Prereqs
Write-Host "`n[1/4] Checking prerequisites..."
try { $node = (node -v); Write-Host "  node $node" } catch { Write-Host "  ! Node.js not found — install Node 20+ first" -ForegroundColor Yellow }
try { git lfs install | Out-Null; Write-Host "  git lfs: installed" } catch { Write-Host "  ! git lfs not found — install Git LFS (needed for art assets)" -ForegroundColor Yellow }

# 2) Doctor (verifies claude-flow / MCP / keys)
Write-Host "`n[2/4] Running claude-flow doctor (verifies MCP + env)..."
npx claude-flow@v3alpha doctor

# 3) Install the game-relevant plugins
Write-Host "`n[3/4] Installing Ruflo plugins..."
$plugins = @(
  'ruflo-core','ruflo-swarm','ruflo-sparc','ruflo-rag-memory','ruflo-agentdb',
  'ruflo-adr','ruflo-testgen','ruflo-docs','ruflo-observability','ruflo-cost-tracker',
  'ruflo-security-audit','ruflo-aidefence','ruflo-browser','ruflo-loop-workers',
  'ruflo-autopilot','ruflo-goals','ruflo-jujutsu','ruflo-ddd','ruflo-workflows'
)
foreach ($p in $plugins) {
  Write-Host "  -> $p"
  try { npx claude-flow@v3alpha plugins install "@claude-flow/$p" } catch { Write-Host "    (skip/err: $($_.Exception.Message))" -ForegroundColor DarkYellow }
}

# 4) Done
Write-Host "`n[4/4] Done. Next:" -ForegroundColor Green
Write-Host "  - Open Claude Code rooted in this folder so .claude/agents + .claude/mcp.json load"
Write-Host "  - Ask the studio-orchestrator for a phase (e.g. 'run Phase 1')"
Write-Host "  - npx claude-flow@v3alpha plugins list   # to verify"
