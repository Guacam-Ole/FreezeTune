// Game state
let currentGuessCount = 0;
let maxGuesses = 8;
let currentCategory = '80s';

// LocalStorage key for game state
const STORAGE_KEY = 'freezetune_game_state';

// Get today's date as string for comparison
function getTodayString() {
    const now = new Date();
    return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
}

// Save game state to localStorage
function saveGameState(completed = false, matchData = null) {
    const state = {
        date: getTodayString(),
        category: currentCategory,
        guessCount: currentGuessCount,
        completed: completed,
        match: matchData,
        lastGameResult: lastGameResult
    };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
}

// Load game state from localStorage
function loadGameState() {
    try {
        const saved = localStorage.getItem(STORAGE_KEY);
        if (!saved) return null;

        const state = JSON.parse(saved);
        // Only return state if it's for today and the same category
        if (state.date === getTodayString() && state.category === currentCategory) {
            return state;
        }
        // Clear old state
        localStorage.removeItem(STORAGE_KEY);
        return null;
    } catch (e) {
        console.error('Error loading game state:', e);
        return null;
    }
}

// Clear game state
function clearGameState() {
    localStorage.removeItem(STORAGE_KEY);
}

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
const progressFill = document.getElementById('progress-fill');
const progressText = document.getElementById('progress-text');
const errorMessage = document.getElementById('error-message');

// Success screen elements
const successInterpret = document.getElementById('success-interpret');
const successTitle = document.getElementById('success-title');
const finalGuessCount = document.getElementById('final-guess-count');
const youtubeVideo = document.getElementById('youtube-video');
const shareResultsBtn = document.getElementById('share-results-btn');

// Store the last game result for sharing
let lastGameResult = {
    guesses: 0,
    success: false
};

// Initialize game on page load
window.addEventListener('DOMContentLoaded', () => {
    if (categorySelect) {
        currentCategory = categorySelect.value;
    }
    initializeGame();
});

// Initialize or resume game
async function initializeGame() {
    const savedState = loadGameState();

    if (savedState) {
        // Restore last game result for sharing
        if (savedState.lastGameResult) {
            lastGameResult = savedState.lastGameResult;
        }

        // If game was completed, show the result screen
        if (savedState.completed && savedState.match) {
            currentGuessCount = savedState.guessCount;
            showSuccess(savedState.match, savedState.guessCount);
            return;
        }

        // If game is in progress, resume from saved position
        if (savedState.guessCount > 0) {
            await resumeGame(savedState.guessCount);
            return;
        }
    }

    // No saved state or fresh game, start new
    startNewGame();
}

// Resume game from a specific guess count
async function resumeGame(guessCount) {
    currentGuessCount = guessCount;
    clearFeedback();
    clearInputs();
    hideSuccessScreen();
    showGameScreen();

    try {
        // Use POST with empty values to get the current image
        const guessData = {
            Interpret: '',
            Title: '',
            GuessCount: guessCount
        };

        const response = await fetch(`/image?category=${encodeURIComponent(currentCategory)}`, {
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

        // Load the image for the current guess position
        if (result.nextPictureContents) {
            loadImage(result.nextPictureContents);
            updateProgress(result.nextPicture);
        }
    } catch (error) {
        showError('Failed to resume game. Starting fresh.');
        console.error('Error resuming game:', error);
        startNewGame();
    }
}

// Event listeners
guessForm.addEventListener('submit', handleGuessSubmit);
if (categorySelect) {
    categorySelect.addEventListener('change', handleCategoryChange);
}
if (restartBtn) {
    restartBtn.addEventListener('click', startNewGame);
}
shareResultsBtn.addEventListener('click', shareResults);

// Start a new game
async function startNewGame() {
    currentGuessCount = 0;
    clearFeedback();
    clearInputs();
    hideSuccessScreen();
    showGameScreen();

    try {
        if (categorySelect) {
            currentCategory = categorySelect.value;
        }
        const response = await fetch(`/image?category=${encodeURIComponent(currentCategory)}`);

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

    // Disable submit button
    submitBtn.disabled = true;
    submitBtn.textContent = 'Checking...';

    // Increment guess count for this attempt
    currentGuessCount++;

    try {
        const guessData = {
            Interpret: interpret,
            Title: title,
            GuessCount: currentGuessCount
        };

        const response = await fetch(`/image?category=${encodeURIComponent(currentCategory)}`, {
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

    // Check if the user got it right (or ran out of guesses)
    if (result.match) {
        showSuccess(result.match, result.guesses);
        return;
    }

    // Save game state after each incorrect guess
    saveGameState(false, null);

    // Show feedback for partial correctness
    showFeedback(result.interpretCorrect, result.titleCorrect);

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

    // Store result for sharing
    lastGameResult = {
        guesses: guesses,
        success: guesses < 8
    };

    // Save completed game state
    saveGameState(true, match);

    // Update header based on whether they won or lost
    const successHeader = document.querySelector('.success-header h2');
    const successMessage = document.querySelector('.success-message');
    const guessCountElement = document.querySelector('.guess-count');

    if (guesses >= 8) {
        successHeader.textContent = 'Game Over!';
        successMessage.textContent = 'You ran out of guesses. Here\'s the answer:';
        guessCountElement.textContent = 'Better luck next time!';
    } else {
        successHeader.textContent = 'Congratulations!';
        successMessage.textContent = 'You guessed it correctly!';
        guessCountElement.innerHTML = `You got it in <span id="final-guess-count">${guesses}</span> guess(es)!`;
    }

    successInterpret.textContent = match.interpret;
    successTitle.textContent = match.title;

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

// Get ordinal suffix for numbers (1st, 2nd, 3rd, etc.)
function getOrdinalSuffix(num) {
    const j = num % 10;
    const k = num % 100;
    if (j === 1 && k !== 11) return num + 'st';
    if (j === 2 && k !== 12) return num + 'nd';
    if (j === 3 && k !== 13) return num + 'rd';
    return num + 'th';
}

// Format date as DD.MM.YY
function formatDate() {
    const now = new Date();
    const day = String(now.getDate()).padStart(2, '0');
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const year = String(now.getFullYear()).slice(-2);
    return `${day}.${month}.${year}`;
}

// Generate emoji chain for share text
function generateEmojiChain(guesses, success) {
    const filmEmoji = '🎞️';
    const arrowEmoji = '➜';
    const successEmoji = '🎵';
    const failEmoji = '🔇';

    let chain = filmEmoji;
    for (let i = 1; i < guesses; i++) {
        chain += ` ${arrowEmoji} ${filmEmoji}`;
    }
    chain += ` ${arrowEmoji} ${success ? successEmoji : failEmoji}`;

    return chain;
}

// Generate share text
function generateShareText() {
    const { guesses, success } = lastGameResult;
    const date = formatDate();
    const emojiChain = generateEmojiChain(guesses, success);

    // Add medal for top 3 guesses
    let medal = '';
    if (success) {
        if (guesses === 1) medal = '🥇';
        else if (guesses === 2) medal = '🥈';
        else if (guesses === 3) medal = '🥉';
        else medal='🏅';
    } else {
        medal='🫤';
    }

    let headerText;
    if (success) {
        const ordinal = getOrdinalSuffix(guesses);
        headerText = `${medal} FreezeTune 80s ${ordinal}/8`;
    } else {
        headerText = `${medal} FreezeTune 80s not solved`;
    }

    return `${headerText}\n${emojiChain}\n\nfreezetune.com\n#FreezeTune`;
}

// Share results function
async function shareResults() {
    const shareText = generateShareText();

    try {
        // Try to use Web Share API if available
        if (navigator.share) {
            await navigator.share({
                text: shareText
            });
        } else {
            // Fallback to clipboard
            await navigator.clipboard.writeText(shareText);

            // Update button text temporarily
            const originalText = shareResultsBtn.textContent;
            shareResultsBtn.textContent = 'Copied!';
            shareResultsBtn.style.background = 'linear-gradient(135deg, var(--success-color), #059669)';

            setTimeout(() => {
                shareResultsBtn.textContent = originalText;
                shareResultsBtn.style.background = '';
            }, 2000);
        }
    } catch (error) {
        console.error('Error sharing:', error);
        showError('Failed to copy to clipboard');
    }
}