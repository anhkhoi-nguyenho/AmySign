#include "threadSharedBufferLib.h"
#include <chrono>

StringMessageQueue::StringMessageQueue(size_t max_size) : max_size_(max_size) {}

void StringMessageQueue::put(const std::string& item) {
    std::unique_lock<std::mutex> lock(mtx_);

    cv_not_full_.wait(lock, [this]() { return queue_.size() < max_size_; });

    queue_.push(item);
    cv_not_empty_.notify_one();
}

bool StringMessageQueue::get_with_timeout(std::string& out_item, int timeout_seconds) {
    std::unique_lock<std::mutex> lock(mtx_);

    bool success = cv_not_empty_.wait_for(lock, std::chrono::seconds(timeout_seconds),
                                          [this]() { return !queue_.empty(); });

    if (!success) {
        return false; // Timeout reached, out_item remains unchanged
    }

    // Assign the retrieved value to the parameter
    out_item = queue_.front();
    queue_.pop();

    cv_not_full_.notify_one();
    return true;
}

bool StringMessageQueue::empty() {
    std::lock_guard<std::mutex> lock(mtx_);
    return queue_.empty();
}
