// Game state
let currentGuessCount = 0;
let maxGuesses = 8;
let currentCategory = new URLSearchParams(window.location.search).get('category') || '80s';
let availableCategories = [];

// LocalStorage key for game state (category-specific)
function getStorageKey() {
    return `freezetune_game_state_${currentCategory}`;
}

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
    localStorage.setItem(getStorageKey(), JSON.stringify(state));
}

// Load game state from localStorage
function loadGameState() {
    try {
        const saved = localStorage.getItem(getStorageKey());
        if (!saved) return null;

        const state = JSON.parse(saved);
        // Only return state if it's for today and the same category
        if (state.date === getTodayString() && state.category === currentCategory) {
            return state;
        }
        // Clear old state
        localStorage.removeItem(getStorageKey());
        return null;
    } catch (e) {
        console.error('Error loading game state:', e);
        return null;
    }
}

// Clear game state
function clearGameState() {
    localStorage.removeItem(getStorageKey());
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
const localVideo = document.getElementById('local-video');
const localVideoSource = document.getElementById('local-video-source');
const shareResultsBtn = document.getElementById('share-results-btn');
const imageSummary = document.getElementById('image-summary');
const thumbnailGrid = document.getElementById('thumbnail-grid');

// Modal elements
const imageModal = document.getElementById('image-modal');
const modalImage = document.getElementById('modal-image');
const modalClose = document.getElementById('modal-close');
const modalOverlay = document.querySelector('.modal-overlay');

// Store the last game result for sharing
let lastGameResult = {
    guesses: 0,
    success: false
};

// Initialize game on page load
window.addEventListener('DOMContentLoaded', async () => {
    if (categorySelect) {
        currentCategory = categorySelect.value;
    }
    await loadCategories();
    initializeGame();
});

// Load available categories from server
async function loadCategories() {
    try {
        const response = await fetch('/Image/Categories');
        if (response.ok) {
            availableCategories = await response.json();
        }
    } catch (e) {
        console.error('Error loading categories:', e);
    }
}

// Update quiz navigation buttons based on current category position
function updateQuizNavigation() {
    const quizNavigation = document.getElementById('quiz-navigation');
    const prevBtn = document.getElementById('prev-quiz-btn');
    const nextBtn = document.getElementById('next-quiz-btn');
    const prevLabel = document.getElementById('prev-quiz-label');
    const nextLabel = document.getElementById('next-quiz-label');

    if (!quizNavigation || availableCategories.length <= 1) {
        if (quizNavigation) quizNavigation.classList.add('hidden');
        return;
    }

    const currentIndex = availableCategories.indexOf(currentCategory);
    if (currentIndex === -1) {
        quizNavigation.classList.add('hidden');
        return;
    }

    const hasPrev = currentIndex > 0;
    const hasNext = currentIndex < availableCategories.length - 1;

    // Show navigation container if there's at least one direction to go
    if (hasPrev || hasNext) {
        quizNavigation.classList.remove('hidden');
    } else {
        quizNavigation.classList.add('hidden');
        return;
    }

    // Previous button
    if (hasPrev) {
        const prevCategory = availableCategories[currentIndex - 1];
        prevBtn.href = `game.html?category=${encodeURIComponent(prevCategory)}`;
        prevLabel.textContent = prevCategory;
        prevBtn.classList.remove('hidden');
    } else {
        prevBtn.classList.add('hidden');
    }

    // Next button
    if (hasNext) {
        const nextCategory = availableCategories[currentIndex + 1];
        nextBtn.href = `game.html?category=${encodeURIComponent(nextCategory)}`;
        nextLabel.textContent = nextCategory;
        nextBtn.classList.remove('hidden');
    } else {
        nextBtn.classList.add('hidden');
    }
}

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
        const wasCorrectGuess = result.interpretCorrect && result.titleCorrect;
        const allPictures = result.allPictureContents || result.AllPictureContents;
        showSuccess(result.match, result.guesses, wasCorrectGuess, allPictures);
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

    // Display interpret hint if provided
    const interpretHint = result.interpret;
    if (interpretHint) {
        interpretInput.value = interpretHint;
    } else if (!result.interpretCorrect) {
        interpretInput.value = '';
    }

    // Clear title if incorrect
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
function showSuccess(match, guesses, wasCorrectGuess = null, allPictures = null) {
    gameScreen.classList.add('hidden');
    successScreen.classList.remove('hidden');

    // Determine success: use wasCorrectGuess if provided, otherwise use saved lastGameResult
    const success = wasCorrectGuess !== null ? wasCorrectGuess : lastGameResult.success;

    // Store result for sharing
    lastGameResult = {
        guesses: guesses,
        success: success
    };

    // Save completed game state
    saveGameState(true, match);

    // Update header based on whether they won or lost
    const successHeader = document.querySelector('.success-header h2');
    const successMessage = document.querySelector('.success-message');
    const guessCountElement = document.querySelector('.guess-count');

    if (!success) {
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

    // Check if local video file exists (handle both camelCase and PascalCase)
    const videoFile = match.videoFile || match.VideoFile;
    if (videoFile) {
        // Show local video, hide YouTube iframe
        youtubeVideo.classList.add('hidden');
        localVideo.classList.remove('hidden');

        // Build stream URL with guess parameters for validation
        const streamUrl = `/Image/Stream?category=${encodeURIComponent(currentCategory)}&Interpret=${encodeURIComponent(match.interpret)}&Title=${encodeURIComponent(match.title)}&GuessCount=${guesses}`;
        localVideoSource.src = streamUrl;
        localVideo.load();
    } else {
        // Show YouTube iframe, hide local video
        localVideo.classList.add('hidden');
        youtubeVideo.classList.remove('hidden');

        // Convert YouTube URL to embed URL
        const embedUrl = convertToEmbedUrl(match.url);
        youtubeVideo.src = embedUrl;
    }

    // Display image thumbnails if available
    if (allPictures && allPictures.length > 0) {
        displayThumbnails(allPictures);
        imageSummary.classList.remove('hidden');
    } else {
        imageSummary.classList.add('hidden');
    }

    // Update navigation to other quizzes
    updateQuizNavigation();
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

    return `https://www.youtube-nocookie.com/embed/${videoId}?autoplay=0`;
}

// Hide success screen
function hideSuccessScreen() {
    successScreen.classList.add('hidden');
    youtubeVideo.src = ''; // Stop YouTube playback
    localVideo.pause(); // Stop local video playback
    localVideoSource.src = '';
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

// Calculate score for a quiz based on guess count
// 1st guess: 256, 2nd: 128, 3rd: 64, etc.
function calculateScore(guesses, success) {
    if (!success) return 0;
    return Math.pow(2, 9 - guesses); // 256, 128, 64, 32, 16, 8, 4, 2
}

// Get all completed quizzes for today from localStorage
function getAllTodayQuizzes() {
    const today = getTodayString();
    const quizzes = [];

    // Scan all localStorage keys for today's quiz results
    for (let i = 0; i < localStorage.length; i++) {
        const key = localStorage.key(i);
        if (key && key.startsWith('freezetune_game_state_')) {
            try {
                const state = JSON.parse(localStorage.getItem(key));
                if (state && state.date === today && state.completed) {
                    quizzes.push({
                        category: state.category,
                        guesses: state.guessCount,
                        success: state.lastGameResult ? state.lastGameResult.success : false
                    });
                }
            } catch (e) {
                console.error('Error parsing quiz state:', e);
            }
        }
    }

    // Sort by category name for consistent display
    quizzes.sort((a, b) => a.category.localeCompare(b.category));
    return quizzes;
}

// Format date as DD.MM.YY
function formatDate() {
    const now = new Date();
    const day = String(now.getDate()).padStart(2, '0');
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const year = String(now.getFullYear()).slice(-2);
    return `${day}.${month}.${year}`;
}

// Generate emoji chain for a single quiz
function generateEmojiChain(guesses, success) {
    const filmEmoji = '🎞️';
    const failEmoji = '🔇';

    // Build film strip chain (one per guess/image seen)
    let chain = '';
    for (let i = 0; i < guesses; i++) {
        chain += filmEmoji + ' ';
    }

    // Add result emoji at the end
    if (success) {
        if (guesses === 1) chain += '🥇';
        else if (guesses === 2) chain += '🥈';
        else if (guesses === 3) chain += '🥉';
        else chain += '🏅';
    } else {
        chain += failEmoji;
    }

    return chain;
}

// Generate share text for all completed quizzes today
function generateShareText() {
    const quizzes = getAllTodayQuizzes();
    const date = formatDate();

    // If no completed quizzes, fall back to current game result
    if (quizzes.length === 0) {
        const { guesses, success } = lastGameResult;
        const emojiChain = generateEmojiChain(guesses, success);
        const score = calculateScore(guesses, success);
        return `FreezeTune Quiz ${date}\n\n${currentCategory}: ${emojiChain}\n\nScore: ${score}\n\n#FreezeTune freezetune.com`;
    }

    // Build the share text with all quizzes
    let totalScore = 0;
    let quizLines = [];

    for (const quiz of quizzes) {
        const emojiChain = generateEmojiChain(quiz.guesses, quiz.success);
        const score = calculateScore(quiz.guesses, quiz.success);
        totalScore += score;
        quizLines.push(`${quiz.category}: ${emojiChain}`);
    }

    return `FreezeTune Quiz ${date}\n\n${quizLines.join('\n')}\n\nScore: ${totalScore}\n\n#FreezeTune freezetune.com`;
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

// Display thumbnails in the summary grid
function displayThumbnails(images) {
    thumbnailGrid.innerHTML = '';

    images.forEach((base64Image, index) => {
        const img = document.createElement('img');
        img.src = `data:image/jpeg;base64,${base64Image}`;
        img.alt = `Freeze frame ${index + 1}`;
        img.addEventListener('click', () => openModal(base64Image));
        thumbnailGrid.appendChild(img);
    });
}

// Open modal with full-size image
function openModal(base64Image) {
    modalImage.src = `data:image/jpeg;base64,${base64Image}`;
    imageModal.classList.remove('hidden');
    document.body.style.overflow = 'hidden'; // Prevent scrolling
}

// Close modal
function closeModal() {
    imageModal.classList.add('hidden');
    modalImage.src = '';
    document.body.style.overflow = ''; // Restore scrolling
}

// Modal event listeners
if (modalClose) {
    modalClose.addEventListener('click', closeModal);
}
if (modalOverlay) {
    modalOverlay.addEventListener('click', closeModal);
}
document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape' && !imageModal.classList.contains('hidden')) {
        closeModal();
    }
});
