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
	question varchar(256) not null,
	answer varchar(512) not null
);

create table positions(
	id integer not null primary key,
	name varchar(32) not null
);

create table accesses(
	telegram_id bigint not null primary key,
	position_id integer not null,
	foreign key (position_id) references positions(id)
);

create table open_questions(
	id serial not null,
	telegram_id bigint not null,
	question varchar(256) not null,
	answer varchar(512),
	primary key (id, telegram_id)
);

create table wait_registration(
	login varchar(32) not null,
	name varchar(32) not null,
	surname varchar(32) not null
);

insert into positions (id, name) values (1, 'user'), (2, 'hr');
