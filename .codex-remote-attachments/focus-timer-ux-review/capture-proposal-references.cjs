const { chromium } = require('C:/Users/mklas/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const path = require('path');
const { pathToFileURL } = require('url');
const fs = require('fs');

const out = path.resolve(__dirname, '../../openspec/changes/adopt-dense-frost-design-system/references/selected');
fs.mkdirSync(out, { recursive: true });

(async () => {
  const browser = await chromium.launch({ headless: true, executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  const page = await browser.newPage({ viewport: { width: 820, height: 1250 }, deviceScaleFactor: 2, colorScheme: 'dark' });
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  await page.goto(pathToFileURL(path.join(__dirname, 'design-options-qa.html')).href, { waitUntil: 'networkidle' });
  const frame = await (await page.locator('iframe').elementHandle()).contentFrame();
  await frame.locator('#ft-preview-glass-light').waitFor();
  await frame.evaluate(async () => { await document.fonts.ready; });
  await frame.evaluate(() => {
    const style = document.createElement('style');
    style.textContent = `
      #ft-ui-options .ft-stage { min-height: 780px; height: auto; padding: 28px; }
      #ft-ui-options .ft-type-sample { display: none !important; }
      #ft-ui-options .ft-proposal { --ft-font: 'Segoe UI', sans-serif; --ft-row-gap: 8px; --ft-body-size: 14px; --ft-heading-size: 24px; --ft-helper-size: 12px; --ft-control-height: 36px; --ft-control-radius: 8px; }
      #ft-ui-options .ft-proposal h2, #ft-ui-options .ft-proposal h3 { font-weight: 600; }
      #ft-ui-options .ft-proposal h3 { font-size: 18px; }
      #ft-ui-options .ft-proposal .ft-body { padding: 20px 24px; }
      #ft-ui-options .ft-proposal .ft-sections { gap: 24px; }
      #ft-ui-options .ft-proposal .ft-product-nav { flex-wrap: nowrap; gap: 8px; margin: 12px 24px 0; }
      #ft-ui-options .ft-proposal.ft-nav-underline .ft-nav-item { padding: 8px 4px 10px; gap: 6px; min-height: 44px; font-size: 14px; }
      #ft-ui-options .ft-proposal.ft-nav-underline .ft-nav-item > svg { display: block; width: 18px; height: 18px; flex: 0 0 auto; }
      #ft-ui-options .ft-proposal .ft-toggle { appearance: auto; width: 18px; height: 18px; border-radius: 4px; accent-color: var(--ft-action); }
      #ft-ui-options .ft-proposal .ft-toggle::before { content: none; }
      #ft-ui-options .ft-proposal .ft-peer-cards { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; margin: 20px 0; }
      #ft-ui-options .ft-proposal .ft-peer-card { border: 1px solid color-mix(in srgb, var(--ft-line) 60%, transparent); border-radius: 12px; padding: 16px; background: color-mix(in srgb, var(--ft-inset) 40%, var(--ft-panel)); min-width: 0; }
      #ft-ui-options .ft-proposal .ft-theme-tools { display: flex; align-items: end; flex-wrap: wrap; gap: 8px; }
      #ft-ui-options .ft-proposal .ft-theme-tools label { display: grid; gap: 6px; flex: 1; min-width: 140px; }
      #ft-ui-options .ft-proposal .ft-theme-tools select { width: 100%; }
      #ft-ui-options .ft-proposal .ft-theme-tools .ft-button { padding: 6px 12px; }
      #ft-ui-options .ft-proposal .ft-disclosure { padding: 12px 0; border-top: 1px solid var(--ft-line); }
      #ft-ui-options .ft-proposal .ft-disclosure summary { display: flex; gap: 8px; align-items: center; font-size: 14px; font-weight: 600; }
      #ft-ui-options .ft-proposal .ft-disclosure svg { width: 16px; height: 16px; }
      #ft-ui-options .ft-proposal .ft-footer { padding: 12px 24px; }
      #ft-ui-options .ft-proposal .ft-footer .ft-button { min-width: 70px; }
      #ft-ui-options .ft-proposal .ft-footer .ft-save-state { min-height: 20px; }
      #ft-ui-options .ft-proposal .ft-description { font-size: 12px; }
    `;
    document.head.append(style);
    for (const product of document.querySelectorAll('.ft-product-window')) {
      product.querySelector('.ft-footer').innerHTML = '<span class="ft-save-state"></span><button class="ft-button ft-primary" type="button">OK</button><button class="ft-button" type="button">Apply</button><button class="ft-button" type="button">Cancel</button>';
      const sections = product.querySelector('.ft-sections');
      if (product.querySelector('h2')?.textContent === 'General') {
        const section = document.createElement('section');
        section.className = 'ft-section';
        section.innerHTML = '<h3>Window</h3><div class="ft-section-rows"><label class="ft-setting"><span class="ft-label">Always on top</span><input type="checkbox" class="ft-toggle" checked></label></div>';
        sections.insertBefore(section, sections.lastElementChild);
      }
    }
  });

  async function show(category, index) {
    await frame.locator('#ft-' + category + '-tab').click();
    const next = frame.locator('#ft-' + category + '-panel .viz-carousel-next');
    for (let n = 0; n < index; n++) await next.click();
    return frame.locator('#ft-' + category + '-panel .ft-variant:visible .ft-stage');
  }
  const captures = [];
  for (const [category, index, file] of [
    ['glass', 1, 'dense-frost.png'],
    ['layout', 0, 'compact-rows.png'],
    ['type', 0, 'native-clarity.png']
  ]) {
    const stage = await show(category, index);
    await stage.screenshot({ path: path.join(out, file) });
    captures.push(file);
  }
  await frame.locator('#ft-controls-tab').click();
  await frame.locator('#ft-controls-panel [data-page-link="general"]:visible').click();
  await frame.locator('#ft-controls-panel .ft-variant:visible .ft-stage').screenshot({ path: path.join(out, 'soft-rounded.png') });
  captures.push('soft-rounded.png');

  await frame.locator('#ft-glass-tab').click();
  const stage = frame.locator('#ft-glass-panel .ft-variant:visible .ft-stage');
  await stage.evaluate(stage => {
    const product = stage.querySelector('.ft-product-window');
    product.classList.add('ft-proposal', 'ft-layout-compact', 'ft-type-native', 'ft-nav-underline', 'ft-controls-soft');
    product.style.setProperty('--ft-control-radius', '8px');
    product.style.maxWidth = '640px';
  });
  await stage.screenshot({ path: path.join(out, 'combined-general.png') });
  captures.push('combined-general.png');

  await stage.evaluate(stage => {
    const product = stage.querySelector('.ft-product-window');
    for (const tab of product.querySelectorAll('[data-page-link]')) {
      tab.removeAttribute('aria-current');
      if (tab.dataset.pageLink === 'appearance') tab.setAttribute('aria-current', 'page');
    }
    const slider = (label, value, max = 100, suffix = '%') => '<label class="ft-range-setting"><span class="ft-range-heading"><span class="ft-label">' + label + '</span><output>' + value + suffix + '</output></span><input type="range" min="0" max="' + max + '" value="' + value + '" class="ft-range" aria-label="' + label + '"></label>';
    product.querySelector('.ft-body').innerHTML = `
      <div class="ft-page-heading"><h2>Appearance</h2></div>
      <div class="ft-theme-tools"><label><span class="ft-label">Theme preset</span><select class="ft-field"><option>Dark</option></select></label><button class="ft-button">Import</button><button class="ft-button">Export</button><button class="ft-button">Reset to default</button></div>
      <div class="ft-peer-cards">
        <section class="ft-peer-card"><h3>Widget layout</h3><div class="ft-section-rows">
          <label class="ft-setting"><span class="ft-label">Compact mode</span><input type="checkbox" class="ft-toggle" checked></label>
          ${slider('Scale', 1.5, 3, '×')}
          <p class="ft-description">Adjust the widget size and choose its compact layout.</p>
        </div></section>
        <section class="ft-peer-card"><h3>Widget opacity</h3><div class="ft-section-rows">
          <label class="ft-setting"><span class="ft-label">Background</span><select class="ft-field" style="width: 88px"><option>Off</option><option>Solid</option></select></label>
          ${slider('Background tint opacity', 85)}
          ${slider('Clock opacity', 100)}
          ${slider('Controls opacity', 100)}
          ${slider('Overall widget fade', 100)}
        </div></section>
      </div>
      <details class="ft-disclosure"><summary><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75"><path d="m9 5 7 7-7 7"/></svg>Color palette</summary><p>Existing color editors remain available here.</p></details>
      <details class="ft-disclosure"><summary><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75"><path d="m9 5 7 7-7 7"/></svg>Opacity diagnostics</summary><p>Existing diagnostic information remains available here.</p></details>
      <p class="ft-description">Appearance previews immediately. Apply saves; OK saves and closes. Cancel restores the last applied appearance.</p>
    `;
    stage.style.minHeight = '920px';
  });
  await stage.screenshot({ path: path.join(out, 'combined-appearance.png') });
  captures.push('combined-appearance.png');
  await stage.locator('.ft-peer-cards').screenshot({ path: path.join(out, 'grouped-cards.png') });
  captures.push('grouped-cards.png');
  const geometry = await stage.evaluate(stage => {
    const product = stage.querySelector('.ft-product-window');
    return { windowWidth: product.clientWidth, windowHeight: product.clientHeight, horizontalOverflow: product.scrollWidth > product.clientWidth + 1, navIcons: product.querySelectorAll('.ft-nav-item svg').length, footer: [...product.querySelectorAll('.ft-footer button')].map(x => x.textContent) };
  });
  console.log(JSON.stringify({ output: out, captures, geometry, errors }, null, 2));
  await browser.close();
  if (errors.length || geometry.horizontalOverflow || geometry.navIcons !== 5) process.exitCode = 1;
})().catch(error => { console.error(error); process.exit(1); });
