#pragma once

#include <string>
#include <queue>
#include <mutex>
#include <condition_variable>
#include "threadSharedBufferLib.h"

class StringMessageQueue {
private:
    std::queue<std::string> queue_;
    size_t max_size_;
    std::mutex mtx_;
    std::condition_variable cv_not_full_;
    std::condition_variable cv_not_empty_;

public:
    // Constructor
    explicit StringMessageQueue(size_t max_size);

    // Push a string into the buffer (Blocks if full)
    void put(const std::string& item);

    // Retrieve a string from the buffer via parameter.
    // Returns true if successful, false if it timed out.
    bool get_with_timeout(std::string& out_item, int timeout_seconds);

    // Check if the buffer is currently empty
    bool empty();
};
