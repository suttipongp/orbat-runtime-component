const PACKAGE_ROOT = "../../dist/c4isr-orbat-plugin/";
const AFFILIATIONS = ["friend", "hostile", "neutral", "unknown"];
const OUTPUT_SIZES = [32, 48, 96, 256];

const state = {
  renderer: null,
  manifest: null,
  checksums: null,
  definitions: [],
  templates: [],
  selected: null,
  domain: "all",
  query: "",
  lastSvg: "",
};

const elements = {
  packageStatus: document.querySelector("#packageStatus"),
  reloadButton: document.querySelector("#reloadButton"),
  downloadButton: document.querySelector("#downloadButton"),
  templateCount: document.querySelector("#templateCount"),
  templateList: document.querySelector("#templateList"),
  searchInput: document.querySelector("#searchInput"),
  selectedName: document.querySelector("#selectedName"),
  selectedMeta: document.querySelector("#selectedMeta"),
  primarySize: document.querySelector("#primarySize"),
  primaryPreview: document.querySelector("#primaryPreview"),
  renderState: document.querySelector("#renderState"),
  affiliationGrid: document.querySelector("#affiliationGrid"),
  sizeGrid: document.querySelector("#sizeGrid"),
  affiliationSelect: document.querySelector("#affiliationSelect"),
  statusSelect: document.querySelector("#statusSelect"),
  modifier1Select: document.querySelector("#modifier1Select"),
  modifier2Select: document.querySelector("#modifier2Select"),
  echelonSelect: document.querySelector("#echelonSelect"),
  mobilitySelect: document.querySelector("#mobilitySelect"),
  missingRevisionToggle: document.querySelector("#missingRevisionToggle"),
  definitionMetric: document.querySelector("#definitionMetric"),
  templateMetric: document.querySelector("#templateMetric"),
  checksumMetric: document.querySelector("#checksumMetric"),
  capabilitiesOutput: document.querySelector("#capabilitiesOutput"),
  referenceOutput: document.querySelector("#referenceOutput"),
  warningList: document.querySelector("#warningList"),
  fatalError: document.querySelector("#fatalError"),
};

function packageUrl(relativePath) {
  return new URL(PACKAGE_ROOT + relativePath, import.meta.url);
}

async function fetchJson(relativePath) {
  const response = await fetch(packageUrl(relativePath), { cache: "no-store" });
  if (!response.ok) throw new Error(`${relativePath}: HTTP ${response.status}`);
  return response.json();
}

function reference(definition) {
  return {
    contractVersion: definition.contractVersion,
    libraryId: definition.libraryId,
    libraryRevision: definition.libraryRevision,
    definitionId: definition.definitionId,
    definitionRevision: definition.definitionRevision,
  };
}

function definitionKey(definition) {
  return [definition.libraryId, definition.libraryRevision, definition.definitionId, definition.definitionRevision].join("|");
}

function optionLabel(definition) {
  const type = definition.classification?.function || definition.classification?.modifierType || definition.name;
  const variant = definition.classification?.variant;
  return variant ? `${type} · ${variant}` : type;
}

function selectDefinition(select) {
  return state.definitions.find((definition) => definitionKey(definition) === select.value) || null;
}

function buildInstance(affiliation = elements.affiliationSelect.value) {
  if (!state.selected) return null;
  const templateReference = reference(state.selected);
  const components = { mainFunction: templateReference };
  const componentSelections = [
    ["modifier1", elements.modifier1Select],
    ["modifier2", elements.modifier2Select],
    ["echelon", elements.echelonSelect],
    ["mobility", elements.mobilitySelect],
  ];
  for (const [name, select] of componentSelections) {
    const definition = selectDefinition(select);
    if (definition) components[name] = reference(definition);
  }
  const template = { ...templateReference };
  if (elements.missingRevisionToggle.checked) {
    template.definitionRevision = 999999;
    components.mainFunction = template;
  }
  return {
    contractVersion: "1.0",
    id: "web-harness.preview",
    template,
    components,
    domain: state.selected.domain,
    affiliation,
    status: { frame: elements.statusSelect.value, operatingState: state.selected.classification?.operatingState || "Ground" },
    classification: {
      category: state.selected.classification?.category || "",
      function: state.selected.classification?.function || state.selected.name,
      variant: state.selected.classification?.variant || "",
    },
    amplifierValues: {},
  };
}

function renderResult(instance, size) {
  return state.renderer.renderSymbol(instance, { size, definitions: state.definitions });
}

function renderInto(container, result) {
  container.replaceChildren();
  const wrapper = document.createElement("div");
  wrapper.innerHTML = result.svg;
  const svg = wrapper.firstElementChild;
  if (svg) container.append(svg);
}

function setWarnings(warnings) {
  elements.warningList.replaceChildren();
  elements.warningList.classList.toggle("has-warning", warnings.length > 0);
  const values = warnings.length ? warnings : ["None"];
  for (const warning of values) {
    const item = document.createElement("li");
    item.textContent = warning;
    elements.warningList.append(item);
  }
}

function renderPrimary() {
  if (!state.renderer || !state.selected) return;
  const instance = buildInstance();
  const result = renderResult(instance, Number(elements.primarySize.value));
  renderInto(elements.primaryPreview, result);
  state.lastSvg = result.svg;
  elements.downloadButton.disabled = false;
  elements.renderState.replaceChildren();
  const dot = document.createElement("span");
  dot.className = `status-dot ${result.status === "ok" ? "ready" : "error"}`;
  const text = document.createElement("span");
  text.textContent = `${result.status.toUpperCase()} · ${result.width}×${result.height}`;
  elements.renderState.append(dot, text);
  elements.referenceOutput.textContent = JSON.stringify(instance.template, null, 2);
  setWarnings(result.warnings);
  renderMatrices(instance);
}

function matrixItem(title, detail, result) {
  const article = document.createElement("article");
  article.className = "matrix-item";
  const header = document.createElement("header");
  const label = document.createElement("span");
  label.textContent = title;
  const meta = document.createElement("span");
  meta.textContent = detail;
  header.append(label, meta);
  const canvas = document.createElement("div");
  canvas.className = "matrix-canvas";
  renderInto(canvas, result);
  article.append(header, canvas);
  return article;
}

function renderMatrices(instance) {
  elements.affiliationGrid.replaceChildren();
  for (const affiliation of AFFILIATIONS) {
    const next = structuredClone(instance);
    next.affiliation = affiliation;
    const result = renderResult(next, 96);
    elements.affiliationGrid.append(matrixItem(affiliation[0].toUpperCase() + affiliation.slice(1), result.status, result));
  }
  elements.sizeGrid.replaceChildren();
  for (const size of OUTPUT_SIZES) {
    const result = renderResult(instance, size);
    elements.sizeGrid.append(matrixItem(`${size} px`, result.status, result));
  }
}

function fillComponentSelect(select, role, domain) {
  const previous = select.value;
  select.replaceChildren(new Option("None", ""));
  const options = state.definitions
    .filter((definition) => definition.role === role && definition.domain === domain)
    .sort((a, b) => optionLabel(a).localeCompare(optionLabel(b)));
  for (const definition of options) select.add(new Option(optionLabel(definition), definitionKey(definition)));
  if ([...select.options].some((option) => option.value === previous)) select.value = previous;
}

function updateCompositionControls() {
  if (!state.selected) return;
  const domain = state.selected.domain;
  fillComponentSelect(elements.modifier1Select, "modifier-1", domain);
  fillComponentSelect(elements.modifier2Select, "modifier-2", domain);
  fillComponentSelect(elements.echelonSelect, "echelon-indicator", domain);
  fillComponentSelect(elements.mobilitySelect, "mobility-indicator", domain);
  elements.echelonSelect.disabled = domain !== "land-unit";
  elements.mobilitySelect.disabled = domain !== "equipment";
}

function selectTemplate(template) {
  state.selected = template;
  elements.missingRevisionToggle.checked = false;
  elements.selectedName.textContent = template.name;
  elements.selectedMeta.textContent = `${template.domain} · ${template.role} · revision ${template.definitionRevision}`;
  updateCompositionControls();
  renderTemplateList();
  renderPrimary();
}

function filteredTemplates() {
  const query = state.query.trim().toLowerCase();
  return state.templates.filter((template) => {
    if (state.domain !== "all" && template.domain !== state.domain) return false;
    if (!query) return true;
    const searchable = [template.name, template.classification?.category, template.classification?.function, template.classification?.variant].join(" ").toLowerCase();
    return searchable.includes(query);
  });
}

function renderTemplateList() {
  const templates = filteredTemplates();
  elements.templateList.replaceChildren();
  elements.templateCount.textContent = `${templates.length} placeable`;
  if (!templates.length) {
    const empty = document.createElement("div");
    empty.className = "empty-list";
    empty.textContent = "No matching templates";
    elements.templateList.append(empty);
    return;
  }
  let currentDomain = "";
  for (const template of templates) {
    if (template.domain !== currentDomain) {
      currentDomain = template.domain;
      const group = document.createElement("div");
      group.className = "group-label";
      group.textContent = currentDomain === "land-unit" ? "Land unit" : "Equipment";
      elements.templateList.append(group);
    }
    const button = document.createElement("button");
    button.type = "button";
    button.className = `template-row${state.selected?.definitionId === template.definitionId ? " active" : ""}`;
    button.dataset.definitionId = template.definitionId;
    const glyph = document.createElement("span");
    glyph.className = "template-glyph";
    glyph.textContent = template.domain === "land-unit" ? "UNIT" : "EQPT";
    const copy = document.createElement("span");
    copy.className = "template-copy";
    const name = document.createElement("strong");
    name.textContent = template.name;
    const detail = document.createElement("span");
    detail.textContent = [template.classification?.function, template.classification?.variant].filter(Boolean).join(" · ") || template.role;
    copy.append(name, detail);
    button.append(glyph, copy);
    button.addEventListener("click", () => selectTemplate(template));
    elements.templateList.append(button);
  }
}

function setPackageStatus(status, text) {
  elements.packageStatus.replaceChildren();
  const dot = document.createElement("span");
  dot.className = `status-dot ${status}`;
  elements.packageStatus.append(dot, document.createTextNode(text));
}

async function loadPackage() {
  elements.fatalError.hidden = true;
  elements.downloadButton.disabled = true;
  setPackageStatus("loading", "Loading package");
  try {
    const [manifest, checksums] = await Promise.all([fetchJson("manifest.json"), fetchJson("checksums.json")]);
    const renderer = await import(packageUrl(manifest.protocol.entryPoint).href + `?v=${Date.now()}`);
    const definitionPaths = Object.keys(checksums.files).filter((path) => path.startsWith("library/definitions/") && path.endsWith(".json")).sort();
    const definitions = await Promise.all(definitionPaths.map(fetchJson));
    const placeableIds = new Set(manifest.contributions.symbolTemplates.map((template) => template.definition.definitionId));
    state.manifest = manifest;
    state.checksums = checksums;
    state.renderer = renderer;
    state.definitions = definitions;
    state.templates = definitions
      .filter((definition) => definition.placeable && placeableIds.has(definition.definitionId))
      .sort((a, b) => (a.domain === b.domain ? optionLabel(a).localeCompare(optionLabel(b)) : a.domain === "land-unit" ? -1 : 1));
    elements.definitionMetric.textContent = String(definitions.length);
    elements.templateMetric.textContent = String(state.templates.length);
    elements.checksumMetric.textContent = String(Object.keys(checksums.files).length);
    elements.capabilitiesOutput.textContent = JSON.stringify(renderer.getRendererCapabilities(), null, 2);
    setPackageStatus("ready", `${manifest.name} · v${manifest.version}`);
    renderTemplateList();
    const initial = state.templates.find((template) => template.domain === "land-unit") || state.templates[0];
    if (initial) selectTemplate(initial);
  } catch (error) {
    console.error(error);
    setPackageStatus("error", "Package unavailable");
    elements.fatalError.textContent = `ORBAT package could not be loaded: ${error.message}`;
    elements.fatalError.hidden = false;
  }
}

function downloadSvg() {
  if (!state.lastSvg || !state.selected) return;
  const blob = new Blob([state.lastSvg + "\n"], { type: "image/svg+xml" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = `${state.selected.definitionId}.svg`;
  link.click();
  URL.revokeObjectURL(url);
}

document.querySelectorAll("[data-domain]").forEach((button) => button.addEventListener("click", () => {
  state.domain = button.dataset.domain;
  document.querySelectorAll("[data-domain]").forEach((item) => item.classList.toggle("active", item === button));
  renderTemplateList();
}));

document.querySelectorAll("[data-tab]").forEach((button) => button.addEventListener("click", () => {
  const tab = button.dataset.tab;
  document.querySelectorAll("[data-tab]").forEach((item) => { item.classList.toggle("active", item === button); item.setAttribute("aria-selected", String(item === button)); });
  document.querySelectorAll(".tab-panel").forEach((panel) => { const active = panel.id === `${tab}Panel`; panel.classList.toggle("active", active); panel.hidden = !active; });
}));

elements.searchInput.addEventListener("input", () => { state.query = elements.searchInput.value; renderTemplateList(); });
elements.templateList.addEventListener("keydown", (event) => {
  if (!["ArrowDown", "ArrowUp"].includes(event.key)) return;
  const rows = [...elements.templateList.querySelectorAll(".template-row")];
  if (!rows.length) return;
  const current = rows.findIndex((row) => row.classList.contains("active"));
  const next = event.key === "ArrowDown" ? Math.min(rows.length - 1, current + 1) : Math.max(0, current - 1);
  rows[next].focus(); rows[next].click(); event.preventDefault();
});
[elements.primarySize, elements.affiliationSelect, elements.statusSelect, elements.modifier1Select, elements.modifier2Select, elements.echelonSelect, elements.mobilitySelect, elements.missingRevisionToggle].forEach((control) => control.addEventListener("change", renderPrimary));
elements.reloadButton.addEventListener("click", loadPackage);
elements.downloadButton.addEventListener("click", downloadSvg);

loadPackage();
