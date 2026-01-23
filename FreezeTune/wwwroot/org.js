const dateInput = document.getElementById('date');
const apikeyInput = document.getElementById('apikey');
const urlInput = document.getElementById('url');
const downloadBtn = document.getElementById('download-btn');
const interpretInput = document.getElementById('interpret');
const titleInput = document.getElementById('title');
const imagesGrid = document.getElementById('images-grid');
const selectedInfo = document.getElementById('selected-info');
const selectedCount = document.getElementById('selected-count');
const addVideoBtn = document.getElementById('add-video-btn');
const errorMessage = document.getElementById('error-message');

let currentVideo = null;
let selectedImages = [];

window.addEventListener('DOMContentLoaded', loadDate);
downloadBtn.addEventListener('click', handleDownload);
addVideoBtn.addEventListener('click', handleAddVideo);

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
        const video = { ...currentVideo, url: url, date: parseDate(dateInput.value.trim()) };

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

        // Check for error in response (handle both camelCase and PascalCase)
        const errorMsg = result.error || result.Error;
        if (errorMsg) {
            showError(errorMsg);
            return;
        }

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
        const video = { ...currentVideo, date: parseDate(dateInput.value.trim()) };
        const response = await fetch(`/Maintenance/Temp?apiKey=${encodeURIComponent(apiKey)}&category=80s`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(video)
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const images = await response.json();
        imagesGrid.innerHTML = '';
        selectedImages = [];
        updateSelectionInfo();

        const sortedKeys = Object.keys(images).map(Number).sort((a, b) => a - b);
        for (const key of sortedKeys) {
            const wrapper = document.createElement('div');
            wrapper.className = 'image-wrapper';
            wrapper.dataset.key = key;

            const img = document.createElement('img');
            img.src = `data:image/jpeg;base64,${images[key]}`;
            img.alt = `Image ${key}`;

            wrapper.appendChild(img);
            wrapper.addEventListener('click', () => handleImageClick(wrapper, key));
            imagesGrid.appendChild(wrapper);
        }

        selectedInfo.style.display = 'block';
        addVideoBtn.style.display = 'block';
    } catch (error) {
        showError('Failed to load images: ' + error.message);
        console.error('Error loading images:', error);
    }
}

async function handleAddVideo() {
    if (selectedImages.length !== 8) {
        showError('Please select exactly 8 images');
        return;
    }

    const apiKey = apikeyInput.value.trim();
    if (!apiKey) {
        showError('Please enter API key');
        return;
    }

    addVideoBtn.disabled = true;
    addVideoBtn.textContent = 'Adding...';

    try {
        const video = {
            url: urlInput.value.trim(),
            interpret: interpretInput.value.trim(),
            title: titleInput.value.trim(),
            date: parseDate(dateInput.value.trim()),
            imageIds: selectedImages
        };

        const response = await fetch(`/Maintenance/Store?apiKey=${encodeURIComponent(apiKey)}&category=80s`, {
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
        if (result) {
            showError('Video added successfully!');
        }
    } catch (error) {
        showError('Failed to add video: ' + error.message);
        console.error('Error adding video:', error);
    } finally {
        addVideoBtn.disabled = false;
        addVideoBtn.textContent = 'Add Video';
    }
}

function handleImageClick(wrapper, key) {
    const index = selectedImages.indexOf(key);

    if (index !== -1) {
        // Deselect
        selectedImages.splice(index, 1);
        wrapper.classList.remove('selected');
        const badge = wrapper.querySelector('.selection-number');
        if (badge) badge.remove();
        updateAllBadges();
    } else if (selectedImages.length < 8) {
        // Select
        selectedImages.push(key);
        wrapper.classList.add('selected');
        const badge = document.createElement('div');
        badge.className = 'selection-number';
        badge.textContent = selectedImages.length;
        wrapper.appendChild(badge);
    } else {
        showError('You can only select 8 images');
    }

    updateSelectionInfo();
}

function updateAllBadges() {
    const wrappers = imagesGrid.querySelectorAll('.image-wrapper');
    wrappers.forEach(wrapper => {
        const key = parseInt(wrapper.dataset.key);
        const index = selectedImages.indexOf(key);
        const badge = wrapper.querySelector('.selection-number');

        if (index !== -1) {
            if (badge) {
                badge.textContent = index + 1;
            }
        } else {
            if (badge) badge.remove();
            wrapper.classList.remove('selected');
        }
    });
}

function updateSelectionInfo() {
    selectedCount.textContent = selectedImages.length;
}

function formatDate(dateString) {
    const date = new Date(dateString);
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    return `${day}.${month}.${year}`;
}

function parseDate(dateString) {
    const parts = dateString.split('.');
    if (parts.length === 3) {
        return `${parts[2]}-${parts[1]}-${parts[0]}`;
    }
    return dateString;
}

function showError(message) {
    errorMessage.textContent = message;
    errorMessage.classList.remove('hidden');
    setTimeout(() => {
        errorMessage.classList.add('hidden');
    }, 5000);
}
