# Documentation Reorganization Plan

## Complete Markdown Inventory

| # | File | Purpose | Category | New Location |
|--:|------|---------|:--------:|-------------|
| 1 | `README.md` | Project readme | HANDOVER | Keep at root |
| 2 | `docs/handover/README.md` | New master handover index | HANDOVER | ✅ Already created |
| 3 | `ProjectContext/PROJECT_OVERVIEW.md` | Project overview | HANDOVER | `docs/handover/PROJECT_OVERVIEW.md` |
| 4 | `ProjectContext/ARCHITECTURE.md` | System architecture | HANDOVER | `docs/handover/SYSTEM_ARCHITECTURE.md` |
| 5 | `ProjectContext/FEATURES.md` | Feature list | HANDOVER | `docs/handover/FEATURES.md` |
| 6 | `ProjectContext/DATABASE.md` | Database schema | HANDOVER | `docs/handover/DATABASE.md` |
| 7 | `ProjectContext/NETWORKING.md` | Network protocol | HANDOVER | `docs/handover/NETWORKING.md` |
| 8 | `ProjectContext/SECURITY.md` | Security architecture | HANDOVER | `docs/handover/SECURITY.md` |
| 9 | `ProjectContext/CLASS_MAP.md` | Class hierarchy | HANDOVER | `docs/handover/CLASS_MAP.md` |
| 10 | `ProjectContext/IMPORTANT_FLOWS.md` | Key workflows | HANDOVER | `docs/handover/IMPORTANT_FLOWS.md` |
| 11 | `ProjectContext/DEFENSE_GUIDE.md` | Defense presentation guide | HANDOVER | `docs/handover/DEFENSE_GUIDE.md` |
| 12 | `docs/DEVELOPER_SETUP.md` | Dev environment setup | HANDOVER | `docs/handover/DEVELOPER_SETUP.md` |
| 13 | `docs/MTLS_SETUP_GUIDE.md` | mTLS certificate setup | HANDOVER | `docs/handover/MTLS_SETUP_GUIDE.md` |
| 14 | `docs/DATABASE_SECRET_SETUP.md` | Database credential setup | HANDOVER | `docs/handover/DATABASE_SECRET_SETUP.md` |
| 15 | `docs/AES_SECRET_SETUP.md` | AES key setup | HANDOVER | `docs/handover/AES_SECRET_SETUP.md` |
| 16 | `docs/PUSH_REQUEST_FLOW.md` | Push request architecture | HANDOVER | `docs/handover/PUSH_REQUEST_FLOW.md` |
| 17 | `docs/YEUCAUDETAI.md` | Requirements (Vietnamese) | HANDOVER | `docs/handover/YEUCAUDETAI.md` |
| 18 | `docs/CHECK_YEUCAUDETAI.md` | Requirements checklist | HANDOVER | `docs/handover/CHECK_YEUCAUDETAI.md` |
| 19 | `docs/SYSTEM_FINAL_AUDIT.md` | Final system audit | CONTEXT | `docs/context/SYSTEM_FINAL_AUDIT.md` |
| 20 | `docs/TESTING_STRATEGY_AUDIT.md` | Testing strategy | CONTEXT | `docs/context/TESTING_STRATEGY.md` |
| 21 | `docs/POST_SECURITY_REFACTOR_AUDIT.md` | Post-refactor security audit | CONTEXT | `docs/context/SECURITY_AUDIT.md` |
| 22 | `docs/MTLS_TEST_REPORT.md` | mTLS test results | CONTEXT | `docs/context/MTLS_TEST_REPORT.md` |
| 23 | `ProjectContext/CHATGPT_CONTEXT_TRANSFER.md` | AI context transfer | CONTEXT | `docs/context/CHATGPT_CONTEXT.md` |
| 24 | `ProjectContext/READ_THIS_FIRST.md` | Quick start for AI | CONTEXT | `docs/context/READ_THIS_FIRST.md` |
| 25 | `SECURITY_AUDIT_MTLS.md` | mTLS security audit (root) | ARCHIVE | `docs/archive/audits/SECURITY_AUDIT_MTLS.md` |
| 26 | `SECURITY_REMEDIATION_AUDIT.md` | Security remediation (root) | ARCHIVE | `docs/archive/remediation/SECURITY_REMEDIATION.md` |
| 27 | `AES_REMEDIATION_PLAN.md` | AES fix plan (root) | ARCHIVE | `docs/archive/remediation/AES_REMEDIATION.md` |
| 28 | `SETUP_SCRIPT_FIX_REPORT.md` | Setup script fix (root) | ARCHIVE | `docs/archive/bugfix/SETUP_SCRIPT_FIX.md` |
| 29 | `PHASE1_ARCHITECTURE_PLAN.md` | Phase 1 plan (root) | ARCHIVE | `docs/archive/phase/PHASE1_PLAN.md` |
| 30 | `PHASE2B_AUDIT_REPORT.md` | Phase 2B audit (root) | ARCHIVE | `docs/archive/audits/PHASE2B_AUDIT.md` |
| 31 | `PLAN_BIDIRECTIONAL_TRANSFER.md` | Bi-directional plan (root) | ARCHIVE | `docs/archive/phase/PLAN_BIDIRECTIONAL.md` |
| 32 | `POWERSHELL_CERT_EXTENSION_AUDIT.md` | Cert extension audit (root) | ARCHIVE | `docs/archive/audits/CERT_EXTENSION_AUDIT.md` |
| 33 | `MTLS_CERTIFICATE_AUDIT.md` | mTLS cert audit (root) | ARCHIVE | `docs/archive/audits/MTLS_CERTIFICATE_AUDIT.md` |
| 34 | `CHAIN_VALIDATION_SECURITY_REVIEW.md` | Chain validation review (root) | ARCHIVE | `docs/archive/audits/CHAIN_VALIDATION.md` |
| 35 | `CA_BASIC_CONSTRAINTS_AUDIT.md` | CA constraints audit (root) | ARCHIVE | `docs/archive/audits/CA_CONSTRAINTS.md` |
| 36 | `PROJECT_ANALYSIS_REPORT.md` | Initial analysis (root) | ARCHIVE | `docs/archive/audits/PROJECT_ANALYSIS.md` |
| 37 | `PROJECT_CONTEXT.md` | Initial context (root) | ARCHIVE | `docs/archive/audits/PROJECT_CONTEXT.md` |
| 38 | `docs/CONNECTION_DROP_ROOT_CAUSE_ANALYSIS.md` | Connection drop audit | ARCHIVE | `docs/archive/audits/CONNECTION_DROP.md` |
| 39 | `docs/CONNECTION_STABILITY_PHASE2_AUDIT.md` | Connection stability audit | ARCHIVE | `docs/archive/audits/CONNECTION_STABILITY.md` |
| 40 | `docs/CONNECTION_STABILITY_FIX_REPORT.md` | Connection stability fix | ARCHIVE | `docs/archive/bugfix/CONNECTION_STABILITY_FIX.md` |
| 41 | `docs/ACTIVE_OFFERS_LIFECYCLE_AUDIT.md` | Active offers audit | ARCHIVE | `docs/archive/audits/ACTIVE_OFFERS.md` |
| 42 | `docs/ACTIVE_OFFERS_FIX_REPORT.md` | Active offers fix | ARCHIVE | `docs/archive/bugfix/ACTIVE_OFFERS_FIX.md` |
| 43 | `docs/MULTI_OFFER_REFACTOR_PLAN.md` | Multi-offer plan | ARCHIVE | `docs/archive/phase/MULTI_OFFER_PLAN.md` |
| 44 | `docs/MULTI_OFFER_THREAD_SAFETY_REVIEW.md` | Multi-offer thread safety review | ARCHIVE | `docs/archive/audits/MULTI_OFFER_THREAD.md` |
| 45 | `docs/MULTI_OFFER_IMPLEMENTATION_REPORT.md` | Multi-offer implementation | ARCHIVE | `docs/archive/phase/MULTI_OFFER_IMPL.md` |
| 46 | `docs/PUSH_REQUEST_PHASE1.md` | Push request phase 1 | ARCHIVE | `docs/archive/phase/PUSH_PHASE1.md` |
| 47 | `docs/PUSH_REQUEST_MULTI_OFFER_AUDIT.md` | Multi-offer audit | ARCHIVE | `docs/archive/audits/PUSH_MULTI_OFFER.md` |
| 48 | `docs/PUSH_DOWNLOAD_UX_IMPROVEMENT.md` | Download UX improvement | ARCHIVE | `docs/archive/bugfix/PUSH_UX.md` |
| 49 | `docs/MULTI_USER_PUSH_AUDIT.md` | Multi-user push audit | ARCHIVE | `docs/archive/audits/MULTI_USER_AUDIT.md` |
| 50 | `docs/MULTI_USER_PUSH_IMPLEMENTATION.md` | Multi-user push impl | ARCHIVE | `docs/archive/phase/MULTI_USER_IMPL.md` |

---

## Proposed Folder Structure

```
docs/
├── handover/
│   ├── README.md                  ← Master index (NEW)
│   ├── SYSTEM_ARCHITECTURE.md     ← from ProjectContext/ARCHITECTURE.md
│   ├── PROJECT_OVERVIEW.md        ← from ProjectContext/PROJECT_OVERVIEW.md
│   ├── FEATURES.md                ← from ProjectContext/FEATURES.md
│   ├── DATABASE.md                ← from ProjectContext/DATABASE.md
│   ├── NETWORKING.md              ← from ProjectContext/NETWORKING.md
│   ├── SECURITY.md                ← from ProjectContext/SECURITY.md
│   ├── CLASS_MAP.md               ← from ProjectContext/CLASS_MAP.md
│   ├── IMPORTANT_FLOWS.md         ← from ProjectContext/IMPORTANT_FLOWS.md
│   ├── DEFENSE_GUIDE.md           ← from ProjectContext/DEFENSE_GUIDE.md
│   ├── DEVELOPER_SETUP.md         ← from docs/DEVELOPER_SETUP.md
│   ├── MTLS_SETUP_GUIDE.md        ← from docs/MTLS_SETUP_GUIDE.md
│   ├── DATABASE_SECRET_SETUP.md   ← from docs/DATABASE_SECRET_SETUP.md
│   ├── AES_SECRET_SETUP.md        ← from docs/AES_SECRET_SETUP.md
│   ├── PUSH_REQUEST_FLOW.md       ← from docs/PUSH_REQUEST_FLOW.md
│   ├── YEUCAUDETAI.md             ← from docs/YEUCAUDETAI.md
│   └── CHECK_YEUCAUDETAI.md       ← from docs/CHECK_YEUCAUDETAI.md
│
├── context/
│   ├── SYSTEM_FINAL_AUDIT.md       ← from docs/SYSTEM_FINAL_AUDIT.md
│   ├── TESTING_STRATEGY.md         ← from docs/TESTING_STRATEGY_AUDIT.md
│   ├── SECURITY_AUDIT.md           ← from docs/POST_SECURITY_REFACTOR_AUDIT.md
│   ├── MTLS_TEST_REPORT.md         ← from docs/MTLS_TEST_REPORT.md
│   ├── CHATGPT_CONTEXT.md          ← from ProjectContext/CHATGPT_CONTEXT_TRANSFER.md
│   └── READ_THIS_FIRST.md          ← from ProjectContext/READ_THIS_FIRST.md
│
└── archive/
    ├── audits/
    │   ├── SECURITY_AUDIT_MTLS.md
    │   ├── PHASE2B_AUDIT.md
    │   ├── CERT_EXTENSION_AUDIT.md
    │   ├── MTLS_CERTIFICATE_AUDIT.md
    │   ├── CHAIN_VALIDATION.md
    │   ├── CA_CONSTRAINTS.md
    │   ├── PROJECT_ANALYSIS.md
    │   ├── PROJECT_CONTEXT.md
    │   ├── CONNECTION_DROP.md
    │   ├── CONNECTION_STABILITY.md
    │   ├── ACTIVE_OFFERS.md
    │   ├── MULTI_OFFER_THREAD.md
    │   ├── PUSH_MULTI_OFFER.md
    │   └── MULTI_USER_AUDIT.md
    │
    ├── remediation/
    │   ├── SECURITY_REMEDIATION.md
    │   └── AES_REMEDIATION.md
    │
    ├── bugfix/
    │   ├── SETUP_SCRIPT_FIX.md
    │   ├── CONNECTION_STABILITY_FIX.md
    │   ├── ACTIVE_OFFERS_FIX.md
    │   └── PUSH_UX.md
    │
    └── phase/
        ├── PHASE1_PLAN.md
        ├── PLAN_BIDIRECTIONAL.md
        ├── MULTI_OFFER_PLAN.md
        ├── MULTI_OFFER_IMPL.md
        ├── PUSH_PHASE1.md
        └── MULTI_USER_IMPL.md
```

---

## Migration Order

### Phase 1: Create directories
```
mkdir docs\handover docs\context docs\archive\audits docs\archive\remediation docs\archive\bugfix docs\archive\phase
```

### Phase 2: Copy files to new locations
```
copy ProjectContext\ARCHITECTURE.md docs\handover\SYSTEM_ARCHITECTURE.md
copy ProjectContext\PROJECT_OVERVIEW.md docs\handover\PROJECT_OVERVIEW.md
copy ProjectContext\FEATURES.md docs\handover\FEATURES.md
... (all HANDOVER category files)

copy docs\SYSTEM_FINAL_AUDIT.md docs\context\SYSTEM_FINAL_AUDIT.md
copy docs\TESTING_STRATEGY_AUDIT.md docs\context\TESTING_STRATEGY.md
... (all CONTEXT category files)

copy SECURITY_AUDIT_MTLS.md docs\archive\audits\SECURITY_AUDIT_MTLS.md
... (all ARCHIVE category files)
```

### Phase 3: Update references
- Update `docs/handover/README.md` to point to new locations
- Update any cross-references between documents

### Phase 4: Verify
- All content preserved
- No broken cross-references
- `docs/handover/README.md` links work

---

## Summary Statistics

| Category | Count | Examples |
|----------|:---:|----------|
| **HANDOVER** | 18 | Architecture, Setup guides, Features, Security |
| **CONTEXT** | 6 | Final audit, Testing strategy, AI context |
| **ARCHIVE (audits)** | 14 | Security audits, connection drop, certificates |
| **ARCHIVE (remediation)** | 2 | Security fix, AES fix |
| **ARCHIVE (bugfix)** | 4 | Setup script, connection stability, active offers |
| **ARCHIVE (phase)** | 6 | Phase plans, multi-offer impl, multi-user impl |
| **Total** | **50** | |