const dateInput = document.getElementById('date');
const apikeyInput = document.getElementById('apikey');
const urlInput = document.getElementById('url');
const downloadBtn = document.getElementById('download-btn');
const interpretInput = document.getElementById('interpret');
const titleInput = document.getElementById('title');
const imagesGrid = document.getElementById('images-grid');
const errorMessage = document.getElementById('error-message');

let currentVideo = null;

window.addEventListener('DOMContentLoaded', loadDate);
downloadBtn.addEventListener('click', handleDownload);

async function loadDate() {
    try {
        const response = await fetch('/Maintenance/Date?category=80s');
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        currentVideo = await response.json();
        dateInput.value = formatDate(currentVideo.date);
    } catch (error) {
        showError('Failed to load date: ' + error.message);
        console.error('Error loading date:', error);
    }
}

async function handleDownload() {
    const apiKey = apikeyInput.value.trim();
    const url = urlInput.value.trim();

    if (!apiKey || !url) {
        showError('Please enter API key and URL');
        return;
    }

    downloadBtn.disabled = true;
    downloadBtn.textContent = 'Downloading...';

    try {
        const video = { ...currentVideo, url: url };

        const response = await fetch(`/Maintenance/Download?apiKey=${encodeURIComponent(apiKey)}&category=80s`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(video)
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();
        interpretInput.value = result.interpret || '';
        titleInput.value = result.title || '';

        await loadTempImages(apiKey);
    } catch (error) {
        showError('Download failed: ' + error.message);
        console.error('Error downloading:', error);
    } finally {
        downloadBtn.disabled = false;
        downloadBtn.textContent = 'Download';
    }
}

async function loadTempImages(apiKey) {
    try {
        const response = await fetch(`/Maintenance/Temp?apiKey=${encodeURIComponent(apiKey)}&category=80s`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(currentVideo)
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const images = await response.json();
        imagesGrid.innerHTML = '';

        const sortedKeys = Object.keys(images).map(Number).sort((a, b) => a - b);
        for (const key of sortedKeys) {
            const img = document.createElement('img');
            img.src = `data:image/jpeg;base64,${images[key]}`;
            img.alt = `Image ${key}`;
            imagesGrid.appendChild(img);
        }
    } catch (error) {
        showError('Failed to load images: ' + error.message);
        console.error('Error loading images:', error);
    }
}

function formatDate(dateString) {
    const date = new Date(dateString);
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    return `${day}.${month}.${year}`;
}

function showError(message) {
    errorMessage.textContent = message;
    errorMessage.classList.remove('hidden');
    setTimeout(() => {
        errorMessage.classList.add('hidden');
    }, 5000);
}