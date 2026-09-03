typedef Json = Map<String, dynamic>;

int jInt(Json j, String k, [int fallback = 0]) =>
    (j[k] as num?)?.toInt() ?? fallback;
double jDouble(Json j, String k, [double fallback = 0]) =>
    (j[k] as num?)?.toDouble() ?? fallback;
String jString(Json j, String k, [String fallback = '']) =>
    j[k]?.toString() ?? fallback;
bool jBool(Json j, String k, [bool fallback = false]) =>
    j[k] as bool? ?? fallback;
DateTime jDate(Json j, String k) =>
    DateTime.tryParse(jString(j, k))?.toLocal() ??
    DateTime.fromMillisecondsSinceEpoch(0);
List<Json> jList(Json j, String k) => (j[k] as List<dynamic>? ?? const [])
    .whereType<Map>()
    .map((e) => Map<String, dynamic>.from(e))
    .toList();

class Paged<T> {
  const Paged(this.items, this.total, this.page, this.pageSize);
  final List<T> items;
  final int total;
  final int page;
  final int pageSize;
  int get pageCount => total == 0 ? 1 : (total / pageSize).ceil();
  factory Paged.fromJson(Json json, T Function(Json) decode) => Paged(
    jList(json, 'items').map(decode).toList(),
    jInt(json, 'total'),
    jInt(json, 'page', 1),
    jInt(json, 'pageSize', 20),
  );
}

class LookupOption {
  const LookupOption(this.id, this.name);
  final int id;
  final String name;
  factory LookupOption.fromJson(Json j) =>
      LookupOption(jInt(j, 'id'), jString(j, 'name'));
}

class ReferenceData {
  const ReferenceData(this.cities, this.categories);
  final List<LookupOption> cities;
  final List<LookupOption> categories;
  factory ReferenceData.fromJson(Json j) => ReferenceData(
    jList(j, 'cities').map(LookupOption.fromJson).toList(),
    jList(j, 'categories').map(LookupOption.fromJson).toList(),
  );
}

class AuthUser {
  const AuthUser({
    required this.id,
    required this.email,
    required this.firstName,
    required this.lastName,
    required this.cityId,
    required this.roles,
  });
  final String id;
  final String email;
  final String firstName;
  final String lastName;
  final int cityId;
  final List<String> roles;
  String get name => '$firstName $lastName'.trim();
  bool get isClient => roles.contains('Client');
  bool get isProfessional => roles.contains('Professional');
  factory AuthUser.fromJson(Json j) => AuthUser(
    id: jString(j, 'id'),
    email: jString(j, 'email'),
    firstName: jString(j, 'firstName'),
    lastName: jString(j, 'lastName'),
    cityId: jInt(j, 'cityId'),
    roles: (j['roles'] as List<dynamic>? ?? const [])
        .map((e) => e.toString())
        .toList(),
  );
}

class AuthSession {
  const AuthSession(this.token, this.refreshToken, this.expiresAt, this.user);
  final String token;
  final String refreshToken;
  final DateTime expiresAt;
  final AuthUser user;
  factory AuthSession.fromJson(Json j) => AuthSession(
    jString(j, 'token'),
    jString(j, 'refreshToken'),
    jDate(j, 'expiresAt'),
    AuthUser.fromJson(Map<String, dynamic>.from(j['user'] as Map)),
  );
}

class UserProfile {
  const UserProfile({
    required this.id,
    required this.email,
    required this.firstName,
    required this.lastName,
    required this.cityId,
    required this.cityName,
    required this.roles,
    this.phone,
    this.picture,
  });
  final String id;
  final String email;
  final String firstName;
  final String lastName;
  final int cityId;
  final String cityName;
  final List<String> roles;
  final String? phone;
  final String? picture;
  String get name => '$firstName $lastName'.trim();
  factory UserProfile.fromJson(Json j) => UserProfile(
    id: jString(j, 'id'),
    email: jString(j, 'email'),
    firstName: jString(j, 'firstName'),
    lastName: jString(j, 'lastName'),
    cityId: jInt(j, 'cityId'),
    cityName: jString(j, 'cityName'),
    roles: (j['roles'] as List<dynamic>? ?? const [])
        .map((e) => e.toString())
        .toList(),
    phone: j['phoneNumber']?.toString(),
    picture: j['profilePictureUrl']?.toString(),
  );
}

class PortfolioItem {
  const PortfolioItem(this.id, this.title, this.description, this.imageUrl);
  final int id;
  final String title;
  final String description;
  final String imageUrl;
  factory PortfolioItem.fromJson(Json j) => PortfolioItem(
    jInt(j, 'id'),
    jString(j, 'title'),
    jString(j, 'description'),
    jString(j, 'imageUrl'),
  );
}

class Professional {
  const Professional({
    required this.id,
    required this.userId,
    required this.name,
    required this.cityId,
    required this.cityName,
    required this.bio,
    required this.hourlyRate,
    required this.experience,
    required this.verified,
    required this.rating,
    required this.categories,
    this.picture,
    this.predictedRating,
    this.personalized = false,
    this.portfolio = const [],
  });
  final int id;
  final String userId;
  final String name;
  final int cityId;
  final String cityName;
  final String bio;
  final double hourlyRate;
  final int experience;
  final bool verified;
  final double rating;
  final List<LookupOption> categories;
  final String? picture;
  final double? predictedRating;
  final bool personalized;
  final List<PortfolioItem> portfolio;
  factory Professional.fromJson(Json j) => Professional(
    id: jInt(j, 'id'),
    userId: jString(j, 'userId'),
    name: '${jString(j, 'firstName')} ${jString(j, 'lastName')}'.trim(),
    cityId: jInt(j, 'cityId'),
    cityName: jString(j, 'cityName'),
    bio: jString(j, 'bio'),
    hourlyRate: jDouble(j, 'hourlyRate'),
    experience: jInt(j, 'yearsOfExperience'),
    verified: jBool(j, 'isVerified'),
    rating: jDouble(j, 'averageRating'),
    categories: jList(j, 'categories').map(LookupOption.fromJson).toList(),
    picture: j['profilePictureUrl']?.toString(),
    predictedRating: (j['predictedRating'] as num?)?.toDouble(),
    personalized: jBool(j, 'isPersonalized'),
    portfolio: jList(j, 'portfolioItems').map(PortfolioItem.fromJson).toList(),
  );
}

class JobPosting {
  const JobPosting({
    required this.id,
    required this.title,
    required this.description,
    required this.budget,
    required this.status,
    required this.categoryId,
    required this.categoryName,
    required this.cityId,
    required this.cityName,
    required this.clientId,
    required this.clientName,
    required this.createdAt,
    this.offerCount = 0,
    this.imageUrls = const [],
  });
  final int id;
  final String title;
  final String description;
  final double budget;
  final int status;
  final int categoryId;
  final String categoryName;
  final int cityId;
  final String cityName;
  final String clientId;
  final String clientName;
  final DateTime createdAt;
  final int offerCount;
  final List<String> imageUrls;
  factory JobPosting.fromJson(Json j) => JobPosting(
    id: jInt(j, 'id'),
    title: jString(j, 'title'),
    description: jString(j, 'description'),
    budget: jDouble(j, 'budget'),
    status: jInt(j, 'status'),
    categoryId: jInt(j, 'categoryId'),
    categoryName: jString(j, 'categoryName'),
    cityId: jInt(j, 'cityId'),
    cityName: jString(j, 'cityName'),
    clientId: jString(j, 'clientUserId'),
    clientName:
        '${jString(j, 'clientFirstName')} ${jString(j, 'clientLastName')}'
            .trim(),
    createdAt: jDate(j, 'createdAt'),
    offerCount: jInt(j, 'offerCount'),
    imageUrls: (j['imageUrls'] as List<dynamic>? ?? const [])
        .map((item) => item.toString())
        .toList(),
  );
}

class JobOffer {
  const JobOffer({
    required this.id,
    required this.jobId,
    required this.professionalId,
    required this.professionalUserId,
    required this.professionalName,
    required this.categoryId,
    required this.categoryName,
    required this.message,
    required this.price,
    required this.status,
    required this.rating,
  });
  final int id;
  final int jobId;
  final int professionalId;
  final String professionalUserId;
  final String professionalName;
  final int categoryId;
  final String categoryName;
  final String message;
  final double price;
  final int status;
  final double rating;
  factory JobOffer.fromJson(Json j) => JobOffer(
    id: jInt(j, 'id'),
    jobId: jInt(j, 'jobPostingId'),
    professionalId: jInt(j, 'professionalProfileId'),
    professionalUserId: jString(j, 'professionalUserId'),
    professionalName:
        '${jString(j, 'professionalFirstName')} ${jString(j, 'professionalLastName')}'
            .trim(),
    categoryId: jInt(j, 'categoryId'),
    categoryName: jString(j, 'categoryName'),
    message: jString(j, 'message'),
    price: jDouble(j, 'proposedPrice'),
    status: jInt(j, 'status'),
    rating: jDouble(j, 'professionalAverageRating'),
  );
}

class AvailableSlot {
  const AvailableSlot(this.start, this.end);
  final DateTime start;
  final DateTime end;
  factory AvailableSlot.fromJson(Json json) =>
      AvailableSlot(jDate(json, 'startUtc'), jDate(json, 'endUtc'));
}

class ProfessionalAvailability {
  const ProfessionalAvailability({
    required this.dayOfWeek,
    required this.startTime,
    required this.endTime,
  });
  final int dayOfWeek;
  final String startTime;
  final String endTime;
  factory ProfessionalAvailability.fromJson(Json json) =>
      ProfessionalAvailability(
        dayOfWeek: jInt(json, 'dayOfWeek'),
        startTime: jString(json, 'startTime'),
        endTime: jString(json, 'endTime'),
      );
}

class Reservation {
  const Reservation({
    required this.id,
    required this.clientId,
    required this.clientName,
    required this.professionalId,
    required this.professionalUserId,
    required this.professionalName,
    required this.categoryId,
    required this.categoryName,
    required this.description,
    required this.scheduledAt,
    required this.duration,
    required this.price,
    required this.status,
    required this.isPaid,
    this.paymentStatus,
    this.reviewId,
    this.reviewRating,
    this.reviewComment,
    this.reviewCreatedAt,
    this.reason,
  });
  final int id;
  final String clientId;
  final String clientName;
  final int professionalId;
  final String professionalUserId;
  final String professionalName;
  final int categoryId;
  final String categoryName;
  final String description;
  final DateTime scheduledAt;
  final int duration;
  final double price;
  final int status;
  final bool isPaid;
  final int? paymentStatus;
  final int? reviewId;
  final int? reviewRating;
  final String? reviewComment;
  final DateTime? reviewCreatedAt;
  final String? reason;
  factory Reservation.fromJson(Json j) => Reservation(
    id: jInt(j, 'id'),
    clientId: jString(j, 'clientUserId'),
    clientName:
        '${jString(j, 'clientFirstName')} ${jString(j, 'clientLastName')}'
            .trim(),
    professionalId: jInt(j, 'professionalProfileId'),
    professionalUserId: jString(j, 'professionalUserId'),
    professionalName:
        '${jString(j, 'professionalFirstName')} ${jString(j, 'professionalLastName')}'
            .trim(),
    categoryId: jInt(j, 'categoryId'),
    categoryName: jString(j, 'categoryName'),
    description: jString(j, 'serviceDescription'),
    scheduledAt: jDate(j, 'scheduledAt'),
    duration: jInt(j, 'durationMinutes'),
    price: jDouble(j, 'totalPrice'),
    status: jInt(j, 'status'),
    isPaid: jBool(j, 'isPaid'),
    paymentStatus: (j['paymentStatus'] as num?)?.toInt(),
    reviewId: (j['reviewId'] as num?)?.toInt(),
    reviewRating: (j['reviewRating'] as num?)?.toInt(),
    reviewComment: j['reviewComment']?.toString(),
    reviewCreatedAt: j['reviewCreatedAt'] == null
        ? null
        : jDate(j, 'reviewCreatedAt'),
    reason: j['cancellationReason']?.toString(),
  );
}

String reservationStatus(int value) => switch (value) {
  1 => 'Na čekanju',
  2 => 'Prihvaćena',
  3 => 'U toku',
  4 => 'Završena',
  5 => 'Otkazana',
  _ => 'Nepoznato',
};

class Review {
  const Review(
    this.id,
    this.reservationId,
    this.clientName,
    this.rating,
    this.comment,
    this.createdAt,
  );
  final int id;
  final int reservationId;
  final String clientName;
  final int rating;
  final String comment;
  final DateTime createdAt;
  factory Review.fromJson(Json j) => Review(
    jInt(j, 'id'),
    jInt(j, 'reservationId'),
    '${jString(j, 'clientFirstName')} ${jString(j, 'clientLastName')}'.trim(),
    jInt(j, 'rating'),
    jString(j, 'comment'),
    jDate(j, 'createdAt'),
  );
}

class Conversation {
  const Conversation(
    this.id,
    this.reservationId,
    this.participants,
    this.lastMessage,
  );
  final int id;
  final int? reservationId;
  final List<ConversationParticipant> participants;
  final ChatMessage? lastMessage;
  factory Conversation.fromJson(Json j) => Conversation(
    jInt(j, 'id'),
    (j['reservationId'] as num?)?.toInt(),
    jList(j, 'participants').map(ConversationParticipant.fromJson).toList(),
    j['lastMessage'] is Map
        ? ChatMessage.fromJson(
            Map<String, dynamic>.from(j['lastMessage'] as Map),
          )
        : null,
  );
}

class ConversationParticipant {
  const ConversationParticipant(this.userId, this.name, this.picture);
  final String userId;
  final String name;
  final String? picture;
  factory ConversationParticipant.fromJson(Json j) => ConversationParticipant(
    jString(j, 'userId'),
    '${jString(j, 'firstName')} ${jString(j, 'lastName')}'.trim(),
    j['profilePictureUrl']?.toString(),
  );
}

class ChatMessage {
  const ChatMessage(
    this.id,
    this.conversationId,
    this.senderId,
    this.senderName,
    this.content,
    this.sentAt,
  );
  final int id;
  final int conversationId;
  final String senderId;
  final String senderName;
  final String content;
  final DateTime sentAt;
  factory ChatMessage.fromJson(Json j) => ChatMessage(
    jInt(j, 'id'),
    jInt(j, 'conversationId'),
    jString(j, 'senderUserId'),
    '${jString(j, 'senderFirstName')} ${jString(j, 'senderLastName')}'.trim(),
    jString(j, 'content'),
    jDate(j, 'sentAt'),
  );
}

class PaymentOrder {
  const PaymentOrder(
    this.paymentId,
    this.reservationId,
    this.orderId,
    this.approvalUrl,
    this.amount,
    this.currency,
  );
  final int paymentId;
  final int reservationId;
  final String orderId;
  final String approvalUrl;
  final double amount;
  final String currency;
  factory PaymentOrder.fromJson(Json j) => PaymentOrder(
    jInt(j, 'paymentId'),
    jInt(j, 'reservationId'),
    jString(j, 'orderId'),
    jString(j, 'approvalUrl'),
    jDouble(j, 'amount'),
    jString(j, 'currency'),
  );
}

class NotificationItem {
  const NotificationItem(
    this.id,
    this.title,
    this.body,
    this.isRead,
    this.createdAt,
    this.type,
  );
  final int id;
  final String title;
  final String body;
  final bool isRead;
  final DateTime createdAt;
  final int type;
  factory NotificationItem.fromJson(Json j) => NotificationItem(
    jInt(j, 'id'),
    jString(j, 'title'),
    jString(j, 'body'),
    jBool(j, 'isRead'),
    jDate(j, 'createdAt'),
    jInt(j, 'type'),
  );
}

class EarningsSummary {
  const EarningsSummary(this.totalRevenue, this.paymentCount, this.currency);
  final double totalRevenue;
  final int paymentCount;
  final String currency;
  factory EarningsSummary.fromJson(Json j) => EarningsSummary(
    jDouble(j, 'totalRevenue'),
    jInt(j, 'paymentCount'),
    jString(j, 'currency'),
  );
}
