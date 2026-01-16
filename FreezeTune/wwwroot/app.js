// Game state
let currentGuessCount = 0;
let maxGuesses = 8;
let currentCategory = '80s';

// DOM elements
const gameScreen = document.getElementById('game-screen');
const successScreen = document.getElementById('success-screen');
const gameImage = document.getElementById('game-image');
const imageLoader = document.getElementById('image-loader');
const guessForm = document.getElementById('guess-form');
const interpretInput = document.getElementById('interpret');
const titleInput = document.getElementById('title');
const interpretFeedback = document.getElementById('interpret-feedback');
const titleFeedback = document.getElementById('title-feedback');
const submitBtn = document.getElementById('submit-btn');
const categorySelect = document.getElementById('category');
const restartBtn = document.getElementById('restart-btn');
const playAgainBtn = document.getElementById('play-again-btn');
const progressFill = document.getElementById('progress-fill');
const progressText = document.getElementById('progress-text');
const errorMessage = document.getElementById('error-message');

// Success screen elements
const successInterpret = document.getElementById('success-interpret');
const successTitle = document.getElementById('success-title');
const finalGuessCount = document.getElementById('final-guess-count');
const youtubeVideo = document.getElementById('youtube-video');

// Initialize game on page load
window.addEventListener('DOMContentLoaded', () => {
    currentCategory = categorySelect.value;
    startNewGame();
});

// Event listeners
guessForm.addEventListener('submit', handleGuessSubmit);
categorySelect.addEventListener('change', handleCategoryChange);
restartBtn.addEventListener('click', startNewGame);
playAgainBtn.addEventListener('click', startNewGame);

// Start a new game
async function startNewGame() {
    currentGuessCount = 0;
    clearFeedback();
    clearInputs();
    hideSuccessScreen();
    showGameScreen();

    try {
        currentCategory = categorySelect.value;
        const response = await fetch(`/Images?category=${encodeURIComponent(currentCategory)}`);

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();
        loadImage(result.nextPictureContents);
        updateProgress(1);
    } catch (error) {
        showError('Failed to load game. Please try again.');
        console.error('Error starting game:', error);
    }
}

// Handle category change
function handleCategoryChange() {
    startNewGame();
}

// Handle guess submission
async function handleGuessSubmit(event) {
    event.preventDefault();

    const interpret = interpretInput.value.trim();
    const title = titleInput.value.trim();

    if (!interpret || !title) {
        showError('Please fill in both fields');
        return;
    }

    // Disable submit button
    submitBtn.disabled = true;
    submitBtn.textContent = 'Checking...';

    try {
        const guessData = {
            interpret: interpret,
            title: title,
            guessCount: currentGuessCount
        };

        const response = await fetch(`/Images?category=${encodeURIComponent(currentCategory)}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(guessData)
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();
        handleGuessResult(result);

    } catch (error) {
        showError('Failed to submit guess. Please try again.');
        console.error('Error submitting guess:', error);
        submitBtn.disabled = false;
        submitBtn.textContent = 'Submit Guess';
    }
}

// Handle the result of a guess
function handleGuessResult(result) {
    currentGuessCount = result.guesses;

    // Check if the user got it right
    if (result.match) {
        showSuccess(result.match, result.guesses);
        return;
    }

    // Show feedback for partial correctness
    showFeedback(result.interpretCorrect, result.titleCorrect);

    // Check if we've reached max guesses
    if (currentGuessCount >= maxGuesses) {
        showError('Game Over! You\'ve used all your guesses.');
        submitBtn.disabled = true;
        return;
    }

    // Load next image
    if (result.nextPictureContents) {
        loadImage(result.nextPictureContents);
        updateProgress(result.nextPicture);
    }

    // Re-enable submit button
    submitBtn.disabled = false;
    submitBtn.textContent = 'Submit Guess';

    // Clear incorrect inputs but keep correct ones
    if (!result.interpretCorrect) {
        interpretInput.value = '';
    }
    if (!result.titleCorrect) {
        titleInput.value = '';
    }

    // Focus on the first incorrect field
    if (!result.interpretCorrect) {
        interpretInput.focus();
    } else if (!result.titleCorrect) {
        titleInput.focus();
    }
}

// Show feedback for correct/incorrect fields
function showFeedback(interpretCorrect, titleCorrect) {
    // Clear previous feedback
    clearFeedback();

    // Interpret feedback
    if (interpretCorrect) {
        interpretInput.classList.add('correct');
        interpretFeedback.textContent = '✓';
        interpretFeedback.classList.add('show');
    } else {
        interpretInput.classList.add('incorrect');
        interpretFeedback.textContent = '✗';
        interpretFeedback.classList.add('show');
    }

    // Title feedback
    if (titleCorrect) {
        titleInput.classList.add('correct');
        titleFeedback.textContent = '✓';
        titleFeedback.classList.add('show');
    } else {
        titleInput.classList.add('incorrect');
        titleFeedback.textContent = '✗';
        titleFeedback.classList.add('show');
    }
}

// Clear feedback
function clearFeedback() {
    interpretInput.classList.remove('correct', 'incorrect', 'partial');
    titleInput.classList.remove('correct', 'incorrect', 'partial');
    interpretFeedback.classList.remove('show');
    titleFeedback.classList.remove('show');
}

// Clear inputs
function clearInputs() {
    interpretInput.value = '';
    titleInput.value = '';
    interpretInput.disabled = false;
    titleInput.disabled = false;
    submitBtn.disabled = false;
    submitBtn.textContent = 'Submit Guess';
}

// Load and display an image
function loadImage(base64Image) {
    imageLoader.classList.remove('hidden');
    gameImage.classList.remove('loaded');

    // Create image URL from base64
    const imageUrl = `data:image/jpeg;base64,${base64Image}`;

    // Load the image
    const img = new Image();
    img.onload = () => {
        gameImage.src = imageUrl;
        gameImage.classList.add('loaded');
        imageLoader.classList.add('hidden');
    };
    img.onerror = () => {
        showError('Failed to load image');
        imageLoader.classList.add('hidden');
    };
    img.src = imageUrl;
}

// Update progress bar
function updateProgress(imageNumber) {
    const percentage = (imageNumber / maxGuesses) * 100;
    progressFill.style.width = `${percentage}%`;
    progressText.textContent = `Image ${imageNumber} of ${maxGuesses}`;
}

// Show success screen
function showSuccess(match, guesses) {
    gameScreen.classList.add('hidden');
    successScreen.classList.remove('hidden');

    successInterpret.textContent = match.interpret;
    successTitle.textContent = match.title;
    finalGuessCount.textContent = guesses;

    // Convert YouTube URL to embed URL
    const embedUrl = convertToEmbedUrl(match.url);
    youtubeVideo.src = embedUrl;
}

// Convert YouTube URL to embed URL
function convertToEmbedUrl(url) {
    // Handle different YouTube URL formats
    let videoId = '';

    if (url.includes('youtube.com/watch?v=')) {
        videoId = url.split('v=')[1].split('&')[0];
    } else if (url.includes('youtu.be/')) {
        videoId = url.split('youtu.be/')[1].split('?')[0];
    } else if (url.includes('youtube.com/embed/')) {
        return url; // Already an embed URL
    }

    return `https://www.youtube.com/embed/${videoId}?autoplay=1`;
}

// Hide success screen
function hideSuccessScreen() {
    successScreen.classList.add('hidden');
    youtubeVideo.src = ''; // Stop video playback
}

// Show game screen
function showGameScreen() {
    gameScreen.classList.remove('hidden');
}

// Show error message
function showError(message) {
    errorMessage.textContent = message;
    errorMessage.classList.remove('hidden');

    setTimeout(() => {
        errorMessage.classList.add('hidden');
    }, 5000);
}