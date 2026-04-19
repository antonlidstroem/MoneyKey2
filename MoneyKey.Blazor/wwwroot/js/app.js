window.renderBarChart = (elementId, categories, income, expenses) => {
    const el = document.getElementById(elementId);
    if (!el) return;
    if (el._chart) { el._chart.destroy(); }
    el._chart = new ApexCharts(el, {
        chart: { type: 'bar', height: 280, toolbar: { show: false }, fontFamily: 'Inter, system-ui, sans-serif' },
        series: [{ name: 'Inkomster', data: income, color: '#2E7D32' }, { name: 'Utgifter', data: expenses, color: '#C62828' }],
        xaxis: { categories },
        yaxis: { labels: { formatter: v => v.toLocaleString('sv-SE') + ' kr' } },
        plotOptions: { bar: { columnWidth: '55%', borderRadius: 4 } },
        legend: { position: 'top' },
        grid: { borderColor: '#E0E0E0' },
        tooltip: { y: { formatter: v => v.toLocaleString('sv-SE', { minimumFractionDigits: 0 }) + ' kr' } }
    });
    el._chart.render();
};

window.renderDonutChart = (elementId, labels, values) => {
    const el = document.getElementById(elementId);
    if (!el) return;
    if (el._chart) { el._chart.destroy(); }
    el._chart = new ApexCharts(el, {
        chart: { type: 'donut', height: 280, toolbar: { show: false }, fontFamily: 'Inter, system-ui, sans-serif' },
        series: values, labels,
        legend: { position: 'bottom' },
        plotOptions: { pie: { donut: { size: '65%' } } },
        tooltip: { y: { formatter: v => v.toLocaleString('sv-SE', { minimumFractionDigits: 0 }) + ' kr' } }
    });
    el._chart.render();
};

window.renderLineChart = (elementId, categories, values) => {
    const el = document.getElementById(elementId);
    if (!el) return;
    if (el._chart) { el._chart.destroy(); }
    const isPositive = values.length === 0 || values[values.length - 1] >= 0;
    el._chart = new ApexCharts(el, {
        chart: { type: 'area', height: 200, toolbar: { show: false }, fontFamily: 'Inter, system-ui, sans-serif', sparkline: { enabled: false } },
        series: [{ name: 'Ackumulerat netto', data: values, color: isPositive ? '#2E7D32' : '#C62828' }],
        xaxis: { categories, labels: { style: { fontSize: '11px' } } },
        yaxis: { labels: { formatter: v => Math.round(v).toLocaleString('sv-SE') + ' kr', style: { fontSize: '10px' } } },
        fill: { type: 'gradient', gradient: { opacityFrom: 0.4, opacityTo: 0.05 } },
        stroke: { width: 2, curve: 'smooth' },
        grid: { borderColor: '#E0E0E0' },
        tooltip: { y: { formatter: v => v.toLocaleString('sv-SE', { minimumFractionDigits: 0 }) + ' kr' } },
        annotations: { yaxis: [{ y: 0, borderColor: '#90A4AE', strokeDashArray: 4 }] }
    });
    el._chart.render();
};

// ── UX helpers ────────────────────────────────────────────────────────────────
window.focusElement = (selector) => {
    const el = typeof selector === 'string'
        ? document.querySelector(selector)
        : document.getElementById(selector);
    if (el) { el.focus(); if (el.select) el.select(); }
};

window.downloadFile = (filename, mimeType, base64Data) => {
    const a    = document.createElement('a');
    a.href     = `data:${mimeType};base64,${base64Data}`;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
};

// Returns elapsed seconds since ISO timestamp
window.getElapsedSeconds = (isoStart) => {
    const ms = Date.now() - new Date(isoStart).getTime();
    return Math.floor(ms / 1000);
};

// Format seconds → "1t 23m" or "45m"
window.formatDuration = (totalSeconds) => {
    const h = Math.floor(totalSeconds / 3600);
    const m = Math.floor((totalSeconds % 3600) / 60);
    const s = totalSeconds % 60;
    if (h > 0) return `${h}t ${m.toString().padStart(2,'0')}m`;
    if (m > 0) return `${m}m ${s.toString().padStart(2,'0')}s`;
    return `${s}s`;
};
