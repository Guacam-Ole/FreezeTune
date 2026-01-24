// Stats page for FreezeTune

const categorySelect = document.getElementById('category');
const statsChart = document.getElementById('stats-chart');
const statsLoader = document.getElementById('stats-loader');
const errorMessage = document.getElementById('error-message');

async function loadCategories() {
    try {
        const response = await fetch('/Image/Categories');
        if (!response.ok) {
            throw new Error('Failed to load categories');
        }

        const categories = await response.json();

        categorySelect.innerHTML = '';
        categories.forEach(category => {
            const option = document.createElement('option');
            option.value = category;
            option.textContent = category;
            categorySelect.appendChild(option);
        });

        // Load stats for the first category
        if (categories.length > 0) {
            loadStats(categories[0]);
        }
    } catch (error) {
        showError('Failed to load categories: ' + error.message);
        console.error('Error loading categories:', error);
    }
}

async function loadStats(category) {
    statsLoader.classList.remove('hidden');
    statsChart.innerHTML = '';

    try {
        const response = await fetch(`/Stats?category=${category}`);
        if (!response.ok) {
            throw new Error('Failed to load statistics');
        }

        const data = await response.json();
        statsLoader.classList.add('hidden');

        if (data.length === 0) {
            statsChart.innerHTML = '<p style="text-align: center; color: var(--text-secondary); padding: 50px;">No statistics available yet.</p>';
            return;
        }

        renderChart(data);
    } catch (error) {
        statsLoader.classList.add('hidden');
        showError(error.message);
    }
}

function calculateAverageGuesses(guessToSuccess) {
    if (!guessToSuccess) return null;

    let totalGuesses = 0;
    let totalSuccesses = 0;

    for (const [guessCount, count] of Object.entries(guessToSuccess)) {
        const numGuesses = parseInt(guessCount);
        totalGuesses += numGuesses * count;
        totalSuccesses += count;
    }

    if (totalSuccesses === 0) return null;
    return Math.round(totalGuesses / totalSuccesses);
}

function renderChart(data) {
    // Sort data by date
    data.sort((a, b) => new Date(a.date) - new Date(b.date));

    const dates = data.map(d => d.date);

    // Calculate average guesses for each day
    const avgGuesses = data.map(d => calculateAverageGuesses(d.guessToSuccess));

    // Average guesses trace (left Y-axis) - blue bars
    const avgGuessesTrace = {
        x: dates,
        y: avgGuesses,
        type: 'bar',
        name: 'Avg. Guesses',
        marker: { color: '#6366f1', opacity: 0.7 },
        yaxis: 'y',
        showlegend: false,
        hovertemplate: 'Avg: %{y}<extra></extra>'
    };

    // Successes trace (right Y-axis) - green
    const successesTrace = {
        x: dates,
        y: data.map(d => d.successes || 0),
        type: 'scatter',
        mode: 'lines+markers',
        name: 'Total Successes',
        line: { color: '#10b981', width: 3 },
        marker: { size: 8 },
        yaxis: 'y2',
        showlegend: false,
        hovertemplate: '%{y}<extra></extra>'
    };

    // Failures trace (right Y-axis) - red
    const failuresTrace = {
        x: dates,
        y: data.map(d => d.failures || 0),
        type: 'scatter',
        mode: 'lines+markers',
        name: 'Total Failures',
        line: { color: '#ef4444', width: 3 },
        marker: { size: 8 },
        yaxis: 'y2',
        showlegend: false,
        hovertemplate: '%{y}<extra></extra>'
    };

    // Total trace (right Y-axis) - yellow/orange
    const totalTrace = {
        x: dates,
        y: data.map(d => (d.successes || 0) + (d.failures || 0)),
        type: 'scatter',
        mode: 'lines+markers',
        name: 'Total Players',
        line: { color: '#f59e0b', width: 3 },
        marker: { size: 8 },
        yaxis: 'y2',
        showlegend: false,
        hovertemplate: '%{y}<extra></extra>'
    };

    const allTraces = [avgGuessesTrace, successesTrace, failuresTrace, totalTrace];

    // Calculate default range (last 7 days)
    const endDate = new Date(dates[dates.length - 1]);
    const startDate = new Date(endDate);
    startDate.setDate(startDate.getDate() - 6);

    const layout = {
        paper_bgcolor: 'rgba(0,0,0,0)',
        plot_bgcolor: 'rgba(0,0,0,0)',
        font: { color: '#f1f5f9' },
        showlegend: false,
        margin: { l: 60, r: 60, t: 30, b: 100 },
        xaxis: {
            title: 'Date',
            gridcolor: '#334155',
            tickformat: '%Y-%m-%d',
            rangeslider: {
                visible: true,
                bgcolor: '#1e293b',
                bordercolor: '#334155'
            },
            range: [startDate.toISOString().split('T')[0], endDate.toISOString().split('T')[0]]
        },
        yaxis: {
            showticklabels: false,
            showgrid: false,
            zeroline: false,
            range: [0, 8]
        },
        yaxis2: {
            title: 'Successes / Failures',
            gridcolor: '#334155',
            overlaying: 'y',
            side: 'right'
        }
    };

    const config = {
        responsive: true,
        displayModeBar: true,
        modeBarButtonsToRemove: ['lasso2d', 'select2d'],
        displaylogo: false
    };

    Plotly.newPlot('stats-chart', allTraces, layout, config);
}

function showError(message) {
    errorMessage.textContent = message;
    errorMessage.classList.remove('hidden');
    setTimeout(() => {
        errorMessage.classList.add('hidden');
    }, 5000);
}

// Event listeners
categorySelect.addEventListener('change', (e) => {
    loadStats(e.target.value);
});

// Initial load - fetch categories first
loadCategories();
