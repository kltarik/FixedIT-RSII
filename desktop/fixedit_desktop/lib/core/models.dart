typedef Json = Map<String, dynamic>;

int jsonInt(Json json, String key, [int fallback = 0]) =>
    (json[key] as num?)?.toInt() ?? fallback;
double jsonDouble(Json json, String key, [double fallback = 0]) =>
    (json[key] as num?)?.toDouble() ?? fallback;
String jsonString(Json json, String key, [String fallback = '']) =>
    json[key]?.toString() ?? fallback;
bool jsonBool(Json json, String key, [bool fallback = false]) =>
    json[key] as bool? ?? fallback;
DateTime jsonDate(Json json, String key) =>
    DateTime.tryParse(jsonString(json, key))?.toLocal() ??
    DateTime.fromMillisecondsSinceEpoch(0);
List<Json> jsonList(Json json, String key) =>
    (json[key] as List<dynamic>? ?? const [])
        .whereType<Map>()
        .map((item) => Map<String, dynamic>.from(item))
        .toList();

class PagedResult<T> {
  const PagedResult({
    required this.items,
    required this.total,
    required this.page,
    required this.pageSize,
  });

  final List<T> items;
  final int total;
  final int page;
  final int pageSize;

  int get pageCount => total == 0 ? 1 : (total / pageSize).ceil();

  factory PagedResult.fromJson(Json json, T Function(Json) decode) =>
      PagedResult(
        items: jsonList(json, 'items').map(decode).toList(),
        total: jsonInt(json, 'total'),
        page: jsonInt(json, 'page', 1),
        pageSize: jsonInt(json, 'pageSize', 20),
      );
}

class CountryRecord {
  const CountryRecord({
    required this.id,
    required this.name,
    required this.code,
  });
  final int id;
  final String name;
  final String code;

  factory CountryRecord.fromJson(Json json) => CountryRecord(
    id: jsonInt(json, 'id'),
    name: jsonString(json, 'name'),
    code: jsonString(json, 'code'),
  );
}

class CityRecord {
  const CityRecord({
    required this.id,
    required this.name,
    required this.countryId,
    required this.countryName,
  });
  final int id;
  final String name;
  final int countryId;
  final String countryName;

  factory CityRecord.fromJson(Json json) => CityRecord(
    id: jsonInt(json, 'id'),
    name: jsonString(json, 'name'),
    countryId: jsonInt(json, 'countryId'),
    countryName: jsonString(json, 'countryName'),
  );
}

class CategoryRecord {
  const CategoryRecord({
    required this.id,
    required this.name,
    required this.description,
    this.iconUrl,
  });
  final int id;
  final String name;
  final String description;
  final String? iconUrl;

  factory CategoryRecord.fromJson(Json json) => CategoryRecord(
    id: jsonInt(json, 'id'),
    name: jsonString(json, 'name'),
    description: jsonString(json, 'description'),
    iconUrl: json['iconUrl']?.toString(),
  );
}

class ReservationStatusRecord {
  const ReservationStatusRecord({
    required this.id,
    required this.name,
    required this.description,
  });
  final int id;
  final String name;
  final String description;

  factory ReservationStatusRecord.fromJson(Json json) =>
      ReservationStatusRecord(
        id: jsonInt(json, 'id'),
        name: jsonString(json, 'name'),
        description: jsonString(json, 'description'),
      );
}

class AdminReviewRecord {
  const AdminReviewRecord({
    required this.id,
    required this.reservationId,
    required this.professionalProfileId,
    required this.clientName,
    required this.professionalName,
    required this.rating,
    required this.comment,
    required this.createdAt,
  });
  final int id;
  final int reservationId;
  final int professionalProfileId;
  final String clientName;
  final String professionalName;
  final int rating;
  final String comment;
  final DateTime createdAt;

  factory AdminReviewRecord.fromJson(Json json) => AdminReviewRecord(
    id: jsonInt(json, 'id'),
    reservationId: jsonInt(json, 'reservationId'),
    professionalProfileId: jsonInt(json, 'professionalProfileId'),
    clientName: jsonString(json, 'clientName'),
    professionalName: jsonString(json, 'professionalName'),
    rating: jsonInt(json, 'rating'),
    comment: jsonString(json, 'comment'),
    createdAt: jsonDate(json, 'createdAt'),
  );
}

class AuthUser {
  const AuthUser({
    required this.id,
    required this.email,
    required this.firstName,
    required this.lastName,
    required this.roles,
  });
  final String id;
  final String email;
  final String firstName;
  final String lastName;
  final List<String> roles;
  String get fullName => '$firstName $lastName'.trim();

  factory AuthUser.fromJson(Json json) => AuthUser(
    id: jsonString(json, 'id'),
    email: jsonString(json, 'email'),
    firstName: jsonString(json, 'firstName'),
    lastName: jsonString(json, 'lastName'),
    roles: (json['roles'] as List<dynamic>? ?? const [])
        .map((role) => role.toString())
        .toList(),
  );
}

class AuthSession {
  const AuthSession({
    required this.token,
    required this.refreshToken,
    required this.expiresAt,
    required this.user,
  });
  final String token;
  final String refreshToken;
  final DateTime expiresAt;
  final AuthUser user;

  factory AuthSession.fromJson(Json json) => AuthSession(
    token: jsonString(json, 'token'),
    refreshToken: jsonString(json, 'refreshToken'),
    expiresAt: jsonDate(json, 'expiresAt'),
    user: AuthUser.fromJson(Map<String, dynamic>.from(json['user'] as Map)),
  );
}

class NamedCount {
  const NamedCount(this.name, this.count);
  final String name;
  final int count;
}

class AdminStats {
  const AdminStats({
    required this.totalUsers,
    required this.activeUsers,
    required this.totalProfessionals,
    required this.totalReservations,
    required this.totalReviews,
    required this.completedPayments,
    required this.totalRevenue,
    required this.currency,
    required this.usersByRole,
    required this.reservationsByStatus,
    required this.generatedAt,
  });
  final int totalUsers;
  final int activeUsers;
  final int totalProfessionals;
  final int totalReservations;
  final int totalReviews;
  final int completedPayments;
  final double totalRevenue;
  final String currency;
  final List<NamedCount> usersByRole;
  final List<NamedCount> reservationsByStatus;
  final DateTime generatedAt;

  factory AdminStats.fromJson(Json json) => AdminStats(
    totalUsers: jsonInt(json, 'totalUsers'),
    activeUsers: jsonInt(json, 'activeUsers'),
    totalProfessionals: jsonInt(json, 'totalProfessionals'),
    totalReservations: jsonInt(json, 'totalReservations'),
    totalReviews: jsonInt(json, 'totalReviews'),
    completedPayments: jsonInt(json, 'completedPayments'),
    totalRevenue: jsonDouble(json, 'totalRevenue'),
    currency: jsonString(json, 'currency'),
    usersByRole: jsonList(json, 'usersByRole')
        .map(
          (item) => NamedCount(
            roleName(jsonString(item, 'role')),
            jsonInt(item, 'count'),
          ),
        )
        .toList(),
    reservationsByStatus: jsonList(json, 'reservationsByStatus')
        .map(
          (item) => NamedCount(
            reservationStatusName(jsonInt(item, 'status')),
            jsonInt(item, 'count'),
          ),
        )
        .toList(),
    generatedAt: jsonDate(json, 'generatedAtUtc'),
  );
}

class AdminUserRecord {
  const AdminUserRecord({
    required this.id,
    required this.email,
    required this.firstName,
    required this.lastName,
    this.phoneNumber,
    required this.cityId,
    required this.cityName,
    required this.isActive,
    required this.roles,
    required this.createdAt,
  });
  final String id;
  final String email;
  final String firstName;
  final String lastName;
  final String? phoneNumber;
  String get name => '$firstName $lastName'.trim();
  final int cityId;
  final String cityName;
  final bool isActive;
  final List<String> roles;
  final DateTime createdAt;
  factory AdminUserRecord.fromJson(Json json) => AdminUserRecord(
    id: jsonString(json, 'id'),
    email: jsonString(json, 'email'),
    firstName: jsonString(json, 'firstName'),
    lastName: jsonString(json, 'lastName'),
    phoneNumber: json['phoneNumber']?.toString(),
    cityId: jsonInt(json, 'cityId'),
    cityName: jsonString(json, 'cityName'),
    isActive: jsonBool(json, 'isActive'),
    roles: (json['roles'] as List<dynamic>? ?? const [])
        .map((role) => role.toString())
        .toList(),
    createdAt: jsonDate(json, 'createdAt'),
  );
}

class CategoryOption {
  const CategoryOption(this.id, this.name);
  final int id;
  final String name;
  factory CategoryOption.fromJson(Json json) =>
      CategoryOption(jsonInt(json, 'id'), jsonString(json, 'name'));
}

class ProfessionalRecord {
  const ProfessionalRecord({
    required this.id,
    required this.userId,
    required this.name,
    required this.cityId,
    required this.cityName,
    required this.bio,
    required this.hourlyRate,
    required this.experience,
    required this.isVerified,
    required this.rating,
    required this.categories,
  });
  final int id;
  final String userId;
  final String name;
  final int cityId;
  final String cityName;
  final String bio;
  final double hourlyRate;
  final int experience;
  final bool isVerified;
  final double rating;
  final List<CategoryOption> categories;
  factory ProfessionalRecord.fromJson(Json json) => ProfessionalRecord(
    id: jsonInt(json, 'id'),
    userId: jsonString(json, 'userId'),
    name: '${jsonString(json, 'firstName')} ${jsonString(json, 'lastName')}'
        .trim(),
    cityId: jsonInt(json, 'cityId'),
    cityName: jsonString(json, 'cityName'),
    bio: jsonString(json, 'bio'),
    hourlyRate: jsonDouble(json, 'hourlyRate'),
    experience: jsonInt(json, 'yearsOfExperience'),
    isVerified: jsonBool(json, 'isVerified'),
    rating: jsonDouble(json, 'averageRating'),
    categories: jsonList(
      json,
      'categories',
    ).map(CategoryOption.fromJson).toList(),
  );
}

class JobRecord {
  const JobRecord({
    required this.id,
    required this.title,
    required this.description,
    required this.budget,
    required this.status,
    required this.categoryId,
    required this.categoryName,
    required this.cityId,
    required this.cityName,
    required this.clientName,
    required this.createdAt,
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
  final String clientName;
  final DateTime createdAt;
  factory JobRecord.fromJson(Json json) => JobRecord(
    id: jsonInt(json, 'id'),
    title: jsonString(json, 'title'),
    description: jsonString(json, 'description'),
    budget: jsonDouble(json, 'budget'),
    status: jsonInt(json, 'status'),
    categoryId: jsonInt(json, 'categoryId'),
    categoryName: jsonString(json, 'categoryName'),
    cityId: jsonInt(json, 'cityId'),
    cityName: jsonString(json, 'cityName'),
    clientName:
        '${jsonString(json, 'clientFirstName')} ${jsonString(json, 'clientLastName')}'
            .trim(),
    createdAt: jsonDate(json, 'createdAt'),
  );
}

class ReservationRecord {
  const ReservationRecord({
    required this.id,
    required this.clientName,
    required this.professionalName,
    required this.serviceDescription,
    required this.scheduledAt,
    required this.durationMinutes,
    required this.totalPrice,
    required this.status,
    required this.isPaid,
    this.cancellationReason,
  });
  final int id;
  final String clientName;
  final String professionalName;
  final String serviceDescription;
  final DateTime scheduledAt;
  final int durationMinutes;
  final double totalPrice;
  final int status;
  final bool isPaid;
  final String? cancellationReason;
  factory ReservationRecord.fromJson(Json json) => ReservationRecord(
    id: jsonInt(json, 'id'),
    clientName:
        '${jsonString(json, 'clientFirstName')} ${jsonString(json, 'clientLastName')}'
            .trim(),
    professionalName:
        '${jsonString(json, 'professionalFirstName')} ${jsonString(json, 'professionalLastName')}'
            .trim(),
    serviceDescription: jsonString(json, 'serviceDescription'),
    scheduledAt: jsonDate(json, 'scheduledAt'),
    durationMinutes: jsonInt(json, 'durationMinutes'),
    totalPrice: jsonDouble(json, 'totalPrice'),
    status: jsonInt(json, 'status'),
    isPaid: jsonBool(json, 'isPaid'),
    cancellationReason: json['cancellationReason']?.toString(),
  );
}

String reservationStatusName(int status) => switch (status) {
  1 => 'Na čekanju',
  2 => 'Prihvaćena',
  3 => 'U toku',
  4 => 'Završena',
  5 => 'Otkazana',
  _ => 'Nepoznato',
};

String roleName(String role) => switch (role) {
  'Admin' => 'Administrator',
  'Client' => 'Klijent',
  'Professional' => 'Profesionalac',
  _ => 'Nepoznata uloga',
};

String auditActionName(String action) => switch (action) {
  'POST' => 'Kreiranje',
  'PUT' => 'Izmjena',
  'DELETE' => 'Brisanje',
  'StatusTransition' => 'Promjena statusa',
  _ => action,
};

String auditEntityName(String entity) => switch (entity) {
  'AdminUsers' => 'Korisnici',
  'Auth' => 'Prijava',
  'Conversations' => 'Razgovori',
  'JobOffers' => 'Ponude za posao',
  'JobPostings' => 'Oglasi za posao',
  'Notifications' => 'Obavijesti',
  'Payments' => 'Plaćanja',
  'Professionals' => 'Profesionalci',
  'Reservations' || 'Reservation' => 'Rezervacije',
  'Reviews' => 'Recenzije',
  'Users' => 'Korisnici',
  'Unknown' || 'Nepoznato' => 'Nepoznato',
  _ => entity,
};

String auditDetailsText(String? details) {
  if (details == null || details.isEmpty) return '';
  return details
      .replaceAll('StatusCode:', 'HTTP status:')
      .replaceAll('Reason:', 'Razlog:')
      .replaceAll('Pending', 'Na čekanju')
      .replaceAll('Accepted', 'Prihvaćena')
      .replaceAll('InProgress', 'U toku')
      .replaceAll('Completed', 'Završena')
      .replaceAll('Cancelled', 'Otkazana');
}

List<int> allowedNextReservationStatuses(int status) => switch (status) {
  1 => const [2, 5],
  2 => const [3, 5],
  3 => const [4, 5],
  _ => const [],
};

class AuditEntry {
  const AuditEntry({
    required this.id,
    required this.userId,
    required this.action,
    required this.entityType,
    required this.entityId,
    required this.ipAddress,
    required this.createdAt,
    this.details,
  });
  final int id;
  final String userId;
  final String action;
  final String entityType;
  final String entityId;
  final String? details;
  final String ipAddress;
  final DateTime createdAt;
  factory AuditEntry.fromJson(Json json) => AuditEntry(
    id: jsonInt(json, 'id'),
    userId: jsonString(json, 'userId'),
    action: jsonString(json, 'action'),
    entityType: jsonString(json, 'entityType'),
    entityId: jsonString(json, 'entityId'),
    details: json['details']?.toString(),
    ipAddress: jsonString(json, 'ipAddress'),
    createdAt: jsonDate(json, 'createdAt'),
  );
}

class RevenuePoint {
  const RevenuePoint({
    required this.year,
    required this.month,
    required this.revenue,
    required this.paymentCount,
  });
  final int year;
  final int month;
  final double revenue;
  final int paymentCount;
  factory RevenuePoint.fromJson(Json json) => RevenuePoint(
    year: jsonInt(json, 'year'),
    month: jsonInt(json, 'month'),
    revenue: jsonDouble(json, 'revenue'),
    paymentCount: jsonInt(json, 'paymentCount'),
  );
}

class CategoryRevenue {
  const CategoryRevenue({
    required this.categoryId,
    required this.categoryName,
    required this.revenue,
    required this.paymentCount,
  });
  final int categoryId;
  final String categoryName;
  final double revenue;
  final int paymentCount;
  factory CategoryRevenue.fromJson(Json json) => CategoryRevenue(
    categoryId: jsonInt(json, 'categoryId'),
    categoryName: jsonString(json, 'categoryName'),
    revenue: jsonDouble(json, 'revenue'),
    paymentCount: jsonInt(json, 'paymentCount'),
  );
}

class FinancialReport {
  const FinancialReport({
    required this.currency,
    required this.totalRevenue,
    required this.paymentCount,
    required this.revenueByMonth,
    required this.revenueByCategory,
    required this.reservations,
  });
  final String currency;
  final double totalRevenue;
  final int paymentCount;
  final List<RevenuePoint> revenueByMonth;
  final List<CategoryRevenue> revenueByCategory;
  final PagedResult<FinancialReservation> reservations;
  factory FinancialReport.fromJson(Json json) => FinancialReport(
    currency: jsonString(json, 'currency'),
    totalRevenue: jsonDouble(json, 'totalRevenue'),
    paymentCount: jsonInt(json, 'paymentCount'),
    revenueByMonth: jsonList(
      json,
      'revenueByMonth',
    ).map(RevenuePoint.fromJson).toList(),
    revenueByCategory: jsonList(
      json,
      'revenueByCategory',
    ).map(CategoryRevenue.fromJson).toList(),
    reservations: PagedResult.fromJson(
      Map<String, dynamic>.from(json['reservations'] as Map),
      FinancialReservation.fromJson,
    ),
  );
}

class FinancialReservation {
  const FinancialReservation({
    required this.paymentId,
    required this.reservationId,
    required this.completedAt,
    required this.clientName,
    required this.professionalName,
    required this.amount,
    required this.currency,
  });
  final int paymentId;
  final int reservationId;
  final DateTime completedAt;
  final String clientName;
  final String professionalName;
  final double amount;
  final String currency;
  factory FinancialReservation.fromJson(Json json) => FinancialReservation(
    paymentId: jsonInt(json, 'paymentId'),
    reservationId: jsonInt(json, 'reservationId'),
    completedAt: jsonDate(json, 'completedAt'),
    clientName: jsonString(json, 'clientName'),
    professionalName: jsonString(json, 'professionalName'),
    amount: jsonDouble(json, 'amount'),
    currency: jsonString(json, 'currency'),
  );
}

class NotificationItem {
  const NotificationItem({
    required this.id,
    required this.title,
    required this.body,
    required this.isRead,
    required this.createdAt,
    required this.type,
  });
  final int id;
  final String title;
  final String body;
  final bool isRead;
  final DateTime createdAt;
  final int type;
  factory NotificationItem.fromJson(Json json) => NotificationItem(
    id: jsonInt(json, 'id'),
    title: jsonString(json, 'title'),
    body: jsonString(json, 'body'),
    isRead: jsonBool(json, 'isRead'),
    createdAt: jsonDate(json, 'createdAt'),
    type: jsonInt(json, 'type'),
  );
}
