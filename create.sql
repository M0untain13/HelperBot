create table employees(
	telegram_id bigint not null primary key,
	login varchar(64) not null,
	name varchar(32) not null,
	surname varchar(32) not null
);

create table mood(
	telegram_id bigint not null,
	survey_date timestamp without time zone not null,
	mark integer not null,
	primary key (telegram_id, survey_date)
);

create table faq(
	id serial not null primary key,
	question varchar(1024) not null,
	answer varchar(1024) not null
);

create table positions(
	id integer not null primary key,
	name varchar(32) not null
);

insert into positions (id, name) values (1, 'user'), (2, 'hr');

create table accesses(
	telegram_id bigint not null primary key,
	position_id integer not null,
	foreign key (position_id) references positions(id)
);

insert into employees values (841493868, 'kart0sh', 'Никита', 'Вершинин');
insert into accesses values (841493868, 2);

create table open_questions(
	question_id serial not null,
	user_telegram_id bigint not null,
	hr_telegram_id bigint not null,
	question varchar(1024) not null,
	answer varchar(1024),
	primary key (question_id, user_telegram_id, hr_telegram_id)
);

create table wait_registration(
	login varchar(32) not null,
	name varchar(32) not null,
	surname varchar(32) not null
);
