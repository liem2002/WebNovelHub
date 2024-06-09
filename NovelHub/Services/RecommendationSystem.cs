using NovelHub.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NovelHub.Services
{
    public class RecommendationSystem
    {
        private NovelHubEntities db = new NovelHubEntities();
        // Tạo một mảng các ký tự đặc biệt bạn muốn loại bỏ
        private char[] punctuationChars = { '~', '`', '!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '-', '_', '=', '+', '[', '{', ']', '}', '\\', '|', ';', ':', '\'', '"', '<', ',', '>', '.', '/', '?' };
        
        private string[] stopWords = {
                "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín", "mười",
                "và", "hoặc", "của", "trong", "ở", "tới", "đến", "cho", "về", "với", "cùng", "là",
                "được", "từ", "đi", "điều", "này", "đó"
            };
        private readonly List<Novel> _novels;
        public RecommendationSystem()
        {
            _novels = db.Novels.ToList();
        }
        // Tiền xử lý văn bản
        private string PreprocessText(string text)
        {
            // Loại bỏ các dấu câu
            foreach (char punctuationChar in punctuationChars)
            {
                text = text.Replace(punctuationChar, ' ');
            }
            // Chuyển đổi văn bản về chữ thường
            text = text.ToLower();
            // Tách các từ và loại bỏ các từ dừng
            var words = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                            .Where(word => !stopWords.Contains(word));
            //.Distinct(); // Loại bỏ các từ trùng lặp
            // Gộp các từ lại thành một chuỗi mới
            string processedText = string.Join(" ", words);
            return processedText;
        }

        // Tính toán giá trị Term Frequency (tần suất xuất hiện của từ) cho một từ trong một văn bản.
        private double ComputeTF(string term, string[] words)
        {
            int termFrequency = words.Count(w => w.Equals(term, StringComparison.OrdinalIgnoreCase));

            // Tính toán và trả về giá trị Term Frequency
            var tf = (double)termFrequency / words.Length;
            return Math.Round(tf, 4);
        }

        // Tính toán giá trị Inverse Document Frequency (tần suất nghịch đảo của từ) cho một từ.
        private double ComputeIDF(string term)
        {
            // Đếm số lượng văn bản chứa từ cần tính IDF
            int documentFrequency = _novels.Count(Novel => PreprocessText(Novel.Description).Contains(term));
            // Tính toán và trả về giá trị IDF
            var idf = Math.Log((double)_novels.Count / (documentFrequency));
            return Math.Round(idf, 4);
        }

        // Tính toán giá trị TF-IDF cho một từ trong một văn bản.
        private double ComputeTFIDF(string term, string[] words)
        {
            double tf = ComputeTF(term, words);
            double idf = ComputeIDF(term);
            double tfidf = tf * idf;
            return Math.Round(tfidf, 4);
        }

        // Tạo vector đặc trưng cho Novel
        private Dictionary<int, double[]> FeatureVector(Novel Novel)
        {
            var words = PreprocessText(Novel.Description).Split(' ');
            var featureVector = new double[words.Length];

            for (int i = 0; i < words.Length; i++)
            {
                var item = ComputeTFIDF(words[i], words);
                featureVector[i] = item;
            }

            var result = new Dictionary<int, double[]>();
            result[Novel.NovelID] = featureVector;
            return result;
        }

        // Tính tích vô hướng của hai vector.
        private double DotProduct(double[] vectorA, double[] vectorB)
        {
            return vectorA.Zip(vectorB, (a, b) => a * b).Sum();
        }

        // Tính toán độ dài (norm) của một vector.
        private double Norm(double[] vector)
        {
            return Math.Sqrt(vector.Sum(x => x * x));
        }

        // Tính cosine similarity
        private double ComputeCosineSimilarityMatrix(double[] vectorA, double[] vectorB)
        {
            var computeCosineSimilarity = DotProduct(vectorA, vectorB) / (Norm(vectorA) * Norm(vectorB));
            return computeCosineSimilarity;
        }
        public IEnumerable<int> FindSimilarNovels(Novel novel, int take)
        {
            var vectorA = FeatureVector(novel);
            var vectorB = vectorA;

            Dictionary<int, double> similarNovelsDictionary = new Dictionary<int, double>();

            foreach (var otherNovel in _novels)
            {
                vectorB = FeatureVector(otherNovel);
                var computeCosine = ComputeCosineSimilarityMatrix(vectorA[novel.NovelID], vectorB[otherNovel.NovelID]);
                similarNovelsDictionary[otherNovel.NovelID] = computeCosine;
            }
            var similarNovels = similarNovelsDictionary
                .OrderByDescending(e => e.Value)
                .Select(e => e.Key)
                .Skip(1)
                .Take(take);
            return similarNovels.ToList();
        }
    }
}