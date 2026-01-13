namespace MVC_project.Services
{
    /// <summary>
    /// Service for secure password hashing and verification using BCrypt.
    /// 
    /// Why BCrypt?
    /// - Built-in salt generation (no need to manage salts separately)
    /// - Adaptive hashing (can increase iterations as hardware improves)
    /// - Computationally expensive to prevent brute-force attacks
    /// - Industry-standard for password security
    /// 
    /// Never store plain text passwords! Always hash before saving to database.
    /// </summary>
    public class PasswordService
    {
        /// <summary>
        /// Hashes a plain text password using BCrypt with automatic salt generation.
        /// 
        /// How it works:
        /// 1. BCrypt generates a random salt
        /// 2. Combines salt + password
        /// 3. Applies expensive hashing algorithm multiple times (work factor)
        /// 4. Returns single string containing: algorithm version + work factor + salt + hash
        /// 
        /// The result looks like: $2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy
        /// - $2a$ = BCrypt algorithm version
        /// - 10 = work factor (2^10 = 1024 iterations)
        /// - Rest = salt + hash combined
        /// 
        /// Same password hashed twice produces different results due to random salt.
        /// This is good - prevents attackers from detecting duplicate passwords.
        /// </summary>
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);  // Generate hash with auto salt
        }

        /// <summary>
        /// Verifies if a plain text password matches a BCrypt hash.
        /// 
        /// How it works:
        /// 1. Extracts salt from the stored hash
        /// 2. Hashes the provided password with extracted salt
        /// 3. Compares result with stored hash using constant-time comparison
        /// 
        /// Constant-time comparison prevents timing attacks where attackers measure
        /// how long verification takes to guess the password character by character.
        /// 
        /// Returns true if password matches, false otherwise.
        /// </summary>
        public bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);  // Safe constant-time comparison
        }

    }
}
