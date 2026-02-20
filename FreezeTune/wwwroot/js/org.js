const categorySelect = document.getElementById('category');
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
const progressContainer = document.getElementById('progress-container');
const progressFill = document.getElementById('progress-fill');
const progressText = document.getElementById('progress-text');

let currentVideo = null;
let selectedImages = [];
let progressInterval = null;

window.addEventListener('DOMContentLoaded', loadCategories);
downloadBtn.addEventListener('click', handleDownload);
addVideoBtn.addEventListener('click', handleAddVideo);
categorySelect.addEventListener('change', () => {
    loadDate();
    loadCaptions();
});

async function loadCategories() {
    try {
        const response = await fetch('/Image/Categories');
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        const categories = await response.json();
        const categoryNames = categories.map(cat => cat.name);

        categorySelect.innerHTML = '';
        categoryNames.forEach(category => {
            const option = document.createElement('option');
            option.value = category;
            option.textContent = category;
            categorySelect.appendChild(option);
        });

        if (categoryNames.length > 0) {
            loadDate();
            loadCaptions();
        }
    } catch (error) {
        showError('Failed to load categories: ' + error.message);
        console.error('Error loading categories:', error);
    }
}

function getSelectedCategory() {
    return categorySelect.value;
}

async function loadCaptions() {
    try {
        const response = await fetch(`/Image/Captions?category=${encodeURIComponent(getSelectedCategory())}`);
        if (response.ok) {
            const captions = await response.json();
            document.querySelector('label[for="interpret"]').textContent = captions.artistCaption || 'Interpret';
            document.querySelector('label[for="title"]').textContent = captions.titleCaption || 'Title';
            interpretInput.closest('.input-group').style.display = captions.hasArtist !== false ? '' : 'none';
        }
    } catch (e) {
        console.error('Error loading captions:', e);
    }
}

async function loadDate() {
    try {
        const response = await fetch(`/Maintenance/Date?category=${encodeURIComponent(getSelectedCategory())}`);
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

function generateSessionId() {
    return 'session-' + Date.now() + '-' + Math.random().toString(36).substring(2, 11);
}

function updateProgress(percent, stage) {
    progressFill.style.width = percent + '%';
    progressText.textContent = `${percent}% - ${stage}`;
}

function startProgressPolling(sessionId) {
    progressContainer.style.display = 'block';
    updateProgress(0, 'Starte...');

    progressInterval = setInterval(async () => {
        try {
            const response = await fetch(`/Maintenance/Progress?sessionId=${encodeURIComponent(sessionId)}`);
            if (response.ok) {
                const progress = await response.json();
                updateProgress(progress.percent, progress.stage);
            }
        } catch (e) {
            // Ignore polling errors
        }
    }, 500);
}

function stopProgressPolling() {
    if (progressInterval) {
        clearInterval(progressInterval);
        progressInterval = null;
    }
    updateProgress(100, 'Fertig');
    setTimeout(() => {
        progressContainer.style.display = 'none';
    }, 1000);
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

    const sessionId = generateSessionId();
    startProgressPolling(sessionId);

    try {
        const video = { ...currentVideo, url: url, date: parseDate(dateInput.value.trim()) };

        const response = await fetch(`/Maintenance/Download?apiKey=${encodeURIComponent(apiKey)}&category=${encodeURIComponent(getSelectedCategory())}&sessionId=${encodeURIComponent(sessionId)}`, {
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
        stopProgressPolling();
        downloadBtn.disabled = false;
        downloadBtn.textContent = 'Download';
    }
}

async function loadTempImages(apiKey) {
    try {
        const video = { ...currentVideo, date: parseDate(dateInput.value.trim()) };
        const response = await fetch(`/Maintenance/Temp?apiKey=${encodeURIComponent(apiKey)}&category=${encodeURIComponent(getSelectedCategory())}`, {
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

        const response = await fetch(`/Maintenance/Store?apiKey=${encodeURIComponent(apiKey)}&category=${encodeURIComponent(getSelectedCategory())}`, {
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
            showError('Video added successfully!', true);
            // Reload the date to get the next available date
            await loadDate();
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

function showError(message, isSuccess = false) {
    errorMessage.textContent = message;
    errorMessage.style.background = isSuccess ? 'var(--success-color)' : '';
    errorMessage.classList.remove('hidden');
    setTimeout(() => {
        errorMessage.classList.add('hidden');
        errorMessage.style.background = '';
    }, 5000);
}
